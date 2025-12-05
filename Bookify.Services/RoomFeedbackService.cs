using Bookify.Core.DTOs;
using Bookify.Core.ViewModels;
using Bookify.Data.Entities;
using Bookify.Data.Interfaces;
using Bookify.Services.Interfaces;

namespace Bookify.Services;

public class RoomFeedbackService : IRoomFeedbackService
{
    private readonly IUnitOfWork _unitOfWork;

    public RoomFeedbackService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RoomFeedbackDto> AddFeedbackAsync(string userId, RoomFeedbackViewModel model)
    {
        var feedback = new RoomFeedback
        {
            UserId = userId,
            RoomId = model.RoomId,
            Comment = model.Comment,
            Rating = model.Rating,
            CreatedAt = DateTime.UtcNow,
            IsApproved = true
        };

        await _unitOfWork.RoomFeedbacks.AddAsync(feedback);
        await _unitOfWork.CommitAsync();

        var room = await _unitOfWork.Rooms.GetWithImagesAsync(model.RoomId);

        return new RoomFeedbackDto
        {
            Id = feedback.Id,
            UserId = feedback.UserId,
            UserName = "", // Will be populated by controller
            UserEmail = "",
            RoomId = feedback.RoomId,
            RoomTypeName = room?.RoomType.Name ?? "",
            Comment = feedback.Comment,
            Rating = feedback.Rating,
            CreatedAt = feedback.CreatedAt,
            IsApproved = feedback.IsApproved
        };
    }

    public async Task<IEnumerable<RoomFeedbackDto>> GetRoomFeedbacksAsync(int roomId, bool approvedOnly = true)
    {
        var feedbacks = await _unitOfWork.RoomFeedbacks.GetAllAsync(f => 
            f.RoomId == roomId && (!approvedOnly || f.IsApproved));
        
        var feedbackList = feedbacks.OrderByDescending(f => f.CreatedAt).ToList();
        var room = await _unitOfWork.Rooms.GetWithImagesAsync(roomId);

        return feedbackList.Select(f => new RoomFeedbackDto
        {
            Id = f.Id,
            UserId = f.UserId,
            UserName = "", // Will be populated by controller
            UserEmail = "",
            RoomId = f.RoomId,
            RoomTypeName = room?.RoomType.Name ?? "",
            Comment = f.Comment,
            Rating = f.Rating,
            CreatedAt = f.CreatedAt,
            IsApproved = f.IsApproved
        });
    }

    public async Task<IEnumerable<RoomFeedbackDto>> GetLatestFeedbacksAsync(int count = 3)
    {
        var feedbacks = await _unitOfWork.RoomFeedbacks.GetAllAsync(f => f.IsApproved);
        var feedbackList = feedbacks.OrderByDescending(f => f.CreatedAt).Take(count).ToList();

        var roomIds = feedbackList.Select(f => f.RoomId).Distinct().ToList();
        var rooms = new Dictionary<int, Room?>();
        foreach (var roomId in roomIds)
        {
            rooms[roomId] = await _unitOfWork.Rooms.GetWithImagesAsync(roomId);
        }

        return feedbackList.Select(f => new RoomFeedbackDto
        {
            Id = f.Id,
            UserId = f.UserId,
            UserName = "", // Will be populated by controller
            UserEmail = "",
            RoomId = f.RoomId,
            RoomTypeName = rooms[f.RoomId]?.RoomType.Name ?? "",
            Comment = f.Comment,
            Rating = f.Rating,
            CreatedAt = f.CreatedAt,
            IsApproved = f.IsApproved
        });
    }

    public async Task<double> GetRoomAverageRatingAsync(int roomId)
    {
        var feedbacks = await _unitOfWork.RoomFeedbacks.GetAllAsync(f => 
            f.RoomId == roomId && f.IsApproved);
        
        if (!feedbacks.Any())
            return 0;

        return feedbacks.Average(f => f.Rating);
    }
}

