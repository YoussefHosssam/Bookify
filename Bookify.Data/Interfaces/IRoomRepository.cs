using Bookify.Core.DTOs;
using Bookify.Core.ViewModels;
using Bookify.Data.Entities;

namespace Bookify.Data.Interfaces;

public interface IRoomRepository : IGenericRepository<Room>
{
    Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, RoomSearchViewModel? filter = null);
    Task<Room?> GetWithImagesAsync(int id);
    Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeBookingId = null);
    Task<IEnumerable<Room>> GetFeaturedRoomsAsync(int count);
}

