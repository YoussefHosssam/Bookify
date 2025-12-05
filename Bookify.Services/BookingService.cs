using Bookify.Core.DTOs;
using Bookify.Core.Enums;
using Bookify.Core.Extensions;
using Bookify.Core.ViewModels;
using Bookify.Data.Entities;
using Bookify.Data.Interfaces;
using Bookify.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Bookify.Services;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _unitOfWork;

    public BookingService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> ValidateCartAvailabilityAsync(CartViewModel cart)
    {
        foreach (var item in cart.Items)
        {
            var isAvailable = await _unitOfWork.Rooms.IsRoomAvailableAsync(
                item.RoomId, item.CheckIn, item.CheckOut);
            
            if (!isAvailable)
                return false;
        }
        return true;
    }

    public async Task<IEnumerable<BookingDto>> CreateBookingsAsync(string userId, CartViewModel cart)
    {
        // Validate availability one more time
        if (!await ValidateCartAvailabilityAsync(cart))
        {
            throw new InvalidOperationException("One or more rooms are no longer available");
        }

        var bookings = new List<BookingDto>();

        // Create a separate booking for each cart item
        foreach (var item in cart.Items)
        {
            var room = await _unitOfWork.Rooms.GetWithImagesAsync(item.RoomId);
            if (room == null)
            {
                throw new InvalidOperationException($"Room {item.RoomId} not found");
            }

            var booking = new Booking
            {
                BookingNumber = StringExtensions.GenerateBookingNumber(),
                UserId = userId,
                RoomId = item.RoomId,
                RoomTypeId = item.RoomTypeId,
                CheckIn = item.CheckIn,
                CheckOut = item.CheckOut,
                Nights = item.Nights,
                TotalAmount = item.SubTotal, // Use item subtotal, not cart total
                Currency = cart.Currency,
                Status = BookingStatus.PendingPayment.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Bookings.AddAsync(booking);
            
            bookings.Add(new BookingDto
            {
                Id = booking.Id,
                BookingNumber = booking.BookingNumber,
                UserId = booking.UserId,
                RoomId = booking.RoomId,
                RoomNumber = room.RoomNumber,
                RoomTypeId = booking.RoomTypeId,
                RoomTypeName = room.RoomType.Name,
                CheckIn = booking.CheckIn,
                CheckOut = booking.CheckOut,
                Nights = booking.Nights,
                TotalAmount = booking.TotalAmount,
                Currency = booking.Currency,
                Status = Enum.Parse<BookingStatus>(booking.Status),
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt
            });
        }

        // Commit all bookings in a single transaction
        await _unitOfWork.CommitAsync();

        return bookings;
    }

    public async Task<IEnumerable<BookingDto>> GetUserBookingsAsync(string userId)
    {
        var bookings = await _unitOfWork.Bookings.GetUserBookingsAsync(userId);
        
        // Mark bookings as Completed if CheckOut date has passed
        var today = DateTime.Today;
        var bookingsToUpdate = bookings.Where(b => 
            b.CheckOut < today && 
            (b.Status == BookingStatus.Confirmed.ToString() || b.Status == BookingStatus.PendingPayment.ToString())
        ).ToList();
        
        foreach (var booking in bookingsToUpdate)
        {
            booking.Status = BookingStatus.Completed.ToString();
            booking.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Bookings.Update(booking);
        }
        
        if (bookingsToUpdate.Any())
        {
            await _unitOfWork.CommitAsync();
        }
        
        return bookings.Select(b => new BookingDto
        {
            Id = b.Id,
            BookingNumber = b.BookingNumber,
            UserId = b.UserId,
            RoomId = b.RoomId,
            RoomNumber = b.Room.RoomNumber,
            RoomTypeId = b.RoomTypeId,
            RoomTypeName = b.RoomType.Name,
            CheckIn = b.CheckIn,
            CheckOut = b.CheckOut,
            Nights = b.Nights,
            TotalAmount = b.TotalAmount,
            Currency = b.Currency,
            Status = Enum.Parse<BookingStatus>(b.Status),
            StripePaymentIntentId = b.StripePaymentIntentId,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt
        }).ToList();
    }

    public async Task<BookingDto?> GetBookingByNumberAsync(string bookingNumber)
    {
        var booking = await _unitOfWork.Bookings.GetByBookingNumberAsync(bookingNumber);
        if (booking == null) return null;

        return new BookingDto
        {
            Id = booking.Id,
            BookingNumber = booking.BookingNumber,
            UserId = booking.UserId,
            RoomId = booking.RoomId,
            RoomNumber = booking.Room.RoomNumber,
            RoomTypeId = booking.RoomTypeId,
            RoomTypeName = booking.RoomType.Name,
            CheckIn = booking.CheckIn,
            CheckOut = booking.CheckOut,
            Nights = booking.Nights,
            TotalAmount = booking.TotalAmount,
            Currency = booking.Currency,
            Status = Enum.Parse<BookingStatus>(booking.Status),
            StripePaymentIntentId = booking.StripePaymentIntentId,
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt
        };
    }

    public async Task<bool> CancelBookingAsync(int bookingId, string userId)
    {
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
        if (booking == null || booking.UserId != userId)
            return false;

        // Check if CheckOut date has passed - if so, mark as Completed instead of allowing cancellation
        var today = DateTime.Today;
        if (booking.CheckOut < today)
        {
            // Mark as Completed if not already
            if (booking.Status != BookingStatus.Completed.ToString())
            {
                booking.Status = BookingStatus.Completed.ToString();
                booking.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Bookings.Update(booking);
                await _unitOfWork.CommitAsync();
            }
            return false; // Cannot cancel completed bookings
        }

        // Only allow cancellation if status is Confirmed or PendingPayment
        if (booking.Status == BookingStatus.Confirmed.ToString() ||
            booking.Status == BookingStatus.PendingPayment.ToString())
        {
            booking.Status = BookingStatus.Cancelled.ToString();
            booking.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Bookings.Update(booking);
            await _unitOfWork.CommitAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> ConfirmPaymentAsync(string bookingNumber, string stripePaymentIntentId)
    {
        var booking = await _unitOfWork.Bookings.GetByBookingNumberAsync(bookingNumber);
        if (booking == null) return false;

        // Check if payment already exists
        var existingPayment = await _unitOfWork.Payments.FirstOrDefaultAsync(
            p => p.ProviderTransactionId == stripePaymentIntentId && p.BookingId == booking.Id);

        if (existingPayment == null)
        {
            // Create payment record
            var payment = new Payment
            {
                BookingId = booking.Id,
                PaymentProvider = PaymentProvider.Stripe.ToString(),
                ProviderTransactionId = stripePaymentIntentId,
                Amount = booking.TotalAmount,
                Currency = booking.Currency,
                Status = PaymentStatus.Succeeded.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Payments.AddAsync(payment);
        }
        else
        {
            // Update existing payment status
            existingPayment.Status = PaymentStatus.Succeeded.ToString();
            _unitOfWork.Payments.Update(existingPayment);
        }

        // Update booking status
        booking.Status = BookingStatus.Confirmed.ToString();
        booking.StripePaymentIntentId = stripePaymentIntentId;
        booking.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Bookings.Update(booking);
        await _unitOfWork.CommitAsync();
        return true;
    }

    public async Task<bool> ConfirmPaymentsAsync(IEnumerable<string> bookingNumbers, string stripePaymentIntentId)
    {
        var success = true;
        foreach (var bookingNumber in bookingNumbers)
        {
            var result = await ConfirmPaymentAsync(bookingNumber, stripePaymentIntentId);
            if (!result)
            {
                success = false;
            }
        }
        return success;
    }
}

