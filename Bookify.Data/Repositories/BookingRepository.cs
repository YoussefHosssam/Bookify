using Bookify.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bookify.Data.Repositories;

public class BookingRepository : GenericRepository<Booking>, IBookingRepository
{
    public BookingRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeBookingId = null)
    {
        var hasOverlappingBooking = await _context.Bookings
            .Where(b => b.RoomId == roomId)
            .Where(b => b.Status == Core.Enums.BookingStatus.Confirmed.ToString() ||
                       b.Status == Core.Enums.BookingStatus.PendingPayment.ToString())
            .Where(b => (b.CheckIn < checkOut && b.CheckOut > checkIn))
            .Where(b => excludeBookingId == null || b.Id != excludeBookingId.Value)
            .AnyAsync();

        return !hasOverlappingBooking;
    }

    public async Task<IEnumerable<Booking>> GetUserBookingsAsync(string userId)
    {
        return await _dbSet
            .Include(b => b.Room)
            .Include(b => b.RoomType)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<Booking?> GetByBookingNumberAsync(string bookingNumber)
    {
        return await _dbSet
            .Include(b => b.Room)
            .Include(b => b.RoomType)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BookingNumber == bookingNumber);
    }
}

