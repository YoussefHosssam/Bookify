using Bookify.Data.Entities;

namespace Bookify.Data.Interfaces;

public interface IBookingRepository : IGenericRepository<Booking>
{
    Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeBookingId = null);
    Task<IEnumerable<Booking>> GetUserBookingsAsync(string userId);
    Task<Booking?> GetByBookingNumberAsync(string bookingNumber);
}

