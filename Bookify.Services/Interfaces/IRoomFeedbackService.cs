using Bookify.Core.DTOs;
using Bookify.Core.ViewModels;

namespace Bookify.Services.Interfaces;

public interface IRoomFeedbackService
{
    Task<RoomFeedbackDto> AddFeedbackAsync(string userId, RoomFeedbackViewModel model);
    Task<IEnumerable<RoomFeedbackDto>> GetRoomFeedbacksAsync(int roomId, bool approvedOnly = true);
    Task<IEnumerable<RoomFeedbackDto>> GetLatestFeedbacksAsync(int count = 3);
    Task<double> GetRoomAverageRatingAsync(int roomId);
}

