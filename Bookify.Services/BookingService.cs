using Bookify.Core.DTOs;
using Bookify.Core.Enums;
using Bookify.Core.Extensions;
using Bookify.Core.ViewModels;
using Bookify.Data.Entities;
using Bookify.Data.Repositories;
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

    public async Task<BookingDto> CreateBookingAsync(string userId, CartViewModel cart)
    {
        // Validate availability one more time
        if (!await ValidateCartAvailabilityAsync(cart))
        {
            throw new InvalidOperationException("One or more rooms are no longer available");
        }

        // For simplicity, create one booking per cart item
        // In a real system, you might want to group by room or create a single booking
        var firstItem = cart.Items.First();
        var room = await _unitOfWork.Rooms.GetWithImagesAsync(firstItem.RoomId);
        if (room == null)
        {
            throw new InvalidOperationException("Room not found");
        }

        var booking = new Booking
        {
            BookingNumber = StringExtensions.GenerateBookingNumber(),
            UserId = userId,
            RoomId = firstItem.RoomId,
            RoomTypeId = firstItem.RoomTypeId,
            CheckIn = firstItem.CheckIn,
            CheckOut = firstItem.CheckOut,
            Nights = firstItem.Nights,
            TotalAmount = cart.TotalAmount,
            Currency = cart.Currency,
            Status = BookingStatus.PendingPayment.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Bookings.AddAsync(booking);
        await _unitOfWork.CommitAsync();

        return new BookingDto
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
        };
    }

    public async Task<IEnumerable<BookingDto>> GetUserBookingsAsync(string userId)
    {
        var bookings = await _unitOfWork.Bookings.GetUserBookingsAsync(userId);
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
            p => p.ProviderTransactionId == stripePaymentIntentId);

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
}

