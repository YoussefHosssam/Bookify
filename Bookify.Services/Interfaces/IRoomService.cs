using Bookify.Core.DTOs;
using Bookify.Core.ViewModels;

namespace Bookify.Services.Interfaces;

public interface IRoomService
{
    Task<PagedResult<RoomDto>> SearchRoomsAsync(RoomSearchViewModel searchModel);
    Task<RoomDto?> GetRoomByIdAsync(int id);
    Task<IEnumerable<RoomTypeDto>> GetAllRoomTypesAsync();
    Task<IEnumerable<RoomDto>> GetFeaturedRoomsAsync(int count = 6);
}

