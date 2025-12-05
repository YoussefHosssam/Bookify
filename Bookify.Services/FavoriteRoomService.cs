using Bookify.Data.Interfaces;
using Bookify.Services.Interfaces;

namespace Bookify.Services;

public class FavoriteRoomService : IFavoriteRoomService
{
    private readonly IUnitOfWork _unitOfWork;

    public FavoriteRoomService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> ToggleFavoriteAsync(string userId, int roomId)
    {
        var existing = await _unitOfWork.FavoriteRooms.FirstOrDefaultAsync(f => 
            f.UserId == userId && f.RoomId == roomId);

        if (existing != null)
        {
            _unitOfWork.FavoriteRooms.Remove(existing);
            await _unitOfWork.CommitAsync();
            return false; // Removed from favorites
        }
        else
        {
            var favorite = new Data.Entities.FavoriteRoom
            {
                UserId = userId,
                RoomId = roomId,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.FavoriteRooms.AddAsync(favorite);
            await _unitOfWork.CommitAsync();
            return true; // Added to favorites
        }
    }

    public async Task<bool> IsFavoriteAsync(string userId, int roomId)
    {
        var favorite = await _unitOfWork.FavoriteRooms.FirstOrDefaultAsync(f => 
            f.UserId == userId && f.RoomId == roomId);
        return favorite != null;
    }

    public async Task<IEnumerable<int>> GetFavoriteRoomIdsAsync(string userId)
    {
        var favorites = await _unitOfWork.FavoriteRooms.GetAllAsync(f => f.UserId == userId);
        return favorites.Select(f => f.RoomId);
    }
}

