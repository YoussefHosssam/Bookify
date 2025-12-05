using Bookify.Core.ViewModels;
using Bookify.Data.Entities;
using Bookify.Data.Interfaces;
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

            // Text search - search in room type name, description, and amenities
            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var searchTerm = filter.SearchText.ToLower();
                query = query.Where(r => 
                    r.RoomType.Name.ToLower().Contains(searchTerm) ||
                    (r.RoomType.Description != null && r.RoomType.Description.ToLower().Contains(searchTerm)) ||
                    (r.RoomType.Amenities != null && r.RoomType.Amenities.ToLower().Contains(searchTerm)) ||
                    r.RoomNumber.Contains(searchTerm)
                );
            }

            // Filter by favorites
            if (filter.FavoritesOnly == true && !string.IsNullOrWhiteSpace(filter.UserId))
            {
                var favoriteRoomIds = await _context.FavoriteRooms
                    .Where(f => f.UserId == filter.UserId)
                    .Select(f => f.RoomId)
                    .ToListAsync();
                query = query.Where(r => favoriteRoomIds.Contains(r.Id));
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

