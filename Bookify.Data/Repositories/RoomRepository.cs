using Bookify.Core.ViewModels;
using Bookify.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bookify.Data.Repositories;

public class RoomRepository : GenericRepository<Room>, IRoomRepository
{
    public RoomRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, RoomSearchViewModel? filter = null)
    {
        var query = _dbSet
            .Include(r => r.RoomType)
            .Include(r => r.Bookings)
            .Include(r => r.Images.OrderBy(img => img.SortOrder))
            .Where(r => r.IsActive);

        // Exclude rooms with overlapping bookings
        var bookedRoomIds = await _context.Bookings
            .Where(b => b.Status == Core.Enums.BookingStatus.Confirmed.ToString() ||
                       b.Status == Core.Enums.BookingStatus.PendingPayment.ToString())
            .Where(b => (b.CheckIn < checkOut && b.CheckOut > checkIn))
            .Select(b => b.RoomId)
            .Distinct()
            .ToListAsync();

        query = query.Where(r => !bookedRoomIds.Contains(r.Id));

        // Apply filters
        if (filter != null)
        {
            if (filter.TypeId.HasValue)
            {
                query = query.Where(r => r.RoomTypeId == filter.TypeId.Value);
            }

            if (filter.PriceMin.HasValue)
            {
                query = query.Where(r => r.RoomType.BasePricePerNight >= filter.PriceMin.Value);
            }

            if (filter.PriceMax.HasValue)
            {
                query = query.Where(r => r.RoomType.BasePricePerNight <= filter.PriceMax.Value);
            }

            if (filter.Adults.HasValue || filter.Children.HasValue)
            {
                var totalGuests = (filter.Adults ?? 0) + (filter.Children ?? 0);
                if (totalGuests > 0)
                {
                    query = query.Where(r => r.RoomType.Capacity >= totalGuests);
                }
            }
        }

        return await query.OrderBy(r => r.RoomType.BasePricePerNight).ToListAsync();
    }

    public async Task<Room?> GetWithImagesAsync(int id)
    {
        return await _dbSet
            .Include(r => r.RoomType)
            .Include(r => r.Images.OrderBy(img => img.SortOrder))
            .FirstOrDefaultAsync(r => r.Id == id);
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

    public async Task<IEnumerable<Room>> GetFeaturedRoomsAsync(int count)
    {
        return await _dbSet
            .Include(r => r.RoomType)
            .Include(r => r.Images.OrderBy(img => img.SortOrder))
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .ToListAsync();
    }
}

