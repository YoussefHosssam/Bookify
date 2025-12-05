namespace Bookify.Services.Interfaces;

public interface IFavoriteRoomService
{
    Task<bool> ToggleFavoriteAsync(string userId, int roomId);
    Task<bool> IsFavoriteAsync(string userId, int roomId);
    Task<IEnumerable<int>> GetFavoriteRoomIdsAsync(string userId);
}

