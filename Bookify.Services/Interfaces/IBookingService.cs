using Bookify.Core.DTOs;
using Bookify.Core.ViewModels;

namespace Bookify.Services.Interfaces;

public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(string userId, CartViewModel cart);
    Task<bool> ValidateCartAvailabilityAsync(CartViewModel cart);
    Task<IEnumerable<BookingDto>> GetUserBookingsAsync(string userId);
    Task<BookingDto?> GetBookingByNumberAsync(string bookingNumber);
    Task<bool> CancelBookingAsync(int bookingId, string userId);
    Task<bool> ConfirmPaymentAsync(string bookingNumber, string stripePaymentIntentId);
}

