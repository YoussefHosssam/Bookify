using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Bookify.Core.ViewModels;
using Bookify.Services.Interfaces;

namespace Bookify.Web.Controllers;

public class RoomsController : Controller
{
    private readonly IRoomService _roomService;
    private readonly IRoomFeedbackService _feedbackService;
    private readonly IFavoriteRoomService _favoriteService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<RoomsController> _logger;

    public RoomsController(
        IRoomService roomService,
        IRoomFeedbackService feedbackService,
        IFavoriteRoomService favoriteService,
        UserManager<IdentityUser> userManager,
        ILogger<RoomsController> logger)
    {
        _roomService = roomService;
        _feedbackService = feedbackService;
        _favoriteService = favoriteService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index(RoomSearchViewModel? searchModel)
    {
        searchModel ??= new RoomSearchViewModel
        {
            CheckIn = DateTime.Today.AddDays(1),
            CheckOut = DateTime.Today.AddDays(2),
            Page = 1,
            PageSize = 12
        };

        searchModel.SortBy = string.IsNullOrWhiteSpace(searchModel.SortBy) ? "popular" : searchModel.SortBy;

        // Set user ID for favorites filter if authenticated
        if (User.Identity?.IsAuthenticated == true && searchModel.FavoritesOnly == true)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            searchModel.UserId = userId;
        }

        var result = await _roomService.SearchRoomsAsync(searchModel);
        ViewBag.RoomTypes = await _roomService.GetAllRoomTypesAsync();
        ViewBag.CheckIn = searchModel.CheckIn?.ToString("yyyy-MM-dd");
        ViewBag.CheckOut = searchModel.CheckOut?.ToString("yyyy-MM-dd");
        ViewBag.TypeId = searchModel.TypeId;
        ViewBag.Adults = searchModel.Adults;
        ViewBag.SortBy = searchModel.SortBy;
        ViewBag.FavoritesOnly = searchModel.FavoritesOnly ?? false;
        
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var room = await _roomService.GetRoomByIdAsync(id);
        if (room == null)
        {
            return NotFound();
        }

        // Get feedbacks for this room
        var feedbacks = await _feedbackService.GetRoomFeedbacksAsync(id, approvedOnly: true);
        var userIds = feedbacks.Select(f => f.UserId).Distinct().ToList();
        var users = new Dictionary<string, IdentityUser?>();
        foreach (var userId in userIds)
        {
            users[userId] = await _userManager.FindByIdAsync(userId);
        }
        foreach (var feedback in feedbacks)
        {
            if (users.ContainsKey(feedback.UserId))
            {
                feedback.UserName = users[feedback.UserId]?.UserName ?? "Anonymous";
                feedback.UserEmail = users[feedback.UserId]?.Email ?? "";
            }
        }
        ViewBag.Feedbacks = feedbacks;
        ViewBag.AverageRating = await _feedbackService.GetRoomAverageRatingAsync(id);

        // Check if user has favorited this room
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                ViewBag.IsFavorite = await _favoriteService.IsFavoriteAsync(userId, id);
            }
        }

        return View(room);
    }

    [HttpGet]
    public async Task<IActionResult> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return RedirectToAction("Index");
        }

        var searchModel = new RoomSearchViewModel
        {
            SearchText = query,
            CheckIn = DateTime.Today.AddDays(1),
            CheckOut = DateTime.Today.AddDays(2),
            Page = 1,
            PageSize = 12,
            SortBy = "popular"
        };

        var result = await _roomService.SearchRoomsAsync(searchModel);
        ViewBag.RoomTypes = await _roomService.GetAllRoomTypesAsync();
        ViewBag.CheckIn = searchModel.CheckIn?.ToString("yyyy-MM-dd");
        ViewBag.CheckOut = searchModel.CheckOut?.ToString("yyyy-MM-dd");
        ViewBag.SearchQuery = query;
        ViewBag.TypeId = searchModel.TypeId;
        ViewBag.Adults = searchModel.Adults;
        ViewBag.SortBy = searchModel.SortBy;
        
        return View("Index", result);
    }

    [HttpPost]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AddFeedback([FromBody] RoomFeedbackViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid feedback data" });
        }

        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not found" });
            }

            var feedback = await _feedbackService.AddFeedbackAsync(userId, model);
            var user = await _userManager.FindByIdAsync(userId);
            feedback.UserName = user?.UserName ?? "Anonymous";
            feedback.UserEmail = user?.Email ?? "";
            return Json(new { success = true, feedback = feedback });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding feedback");
            return Json(new { success = false, message = "Error adding feedback" });
        }
    }

    [HttpPost]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ToggleFavorite([FromBody] ToggleFavoriteRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not found" });
            }

            var isFavorite = await _favoriteService.ToggleFavoriteAsync(userId, request.RoomId);
            return Json(new { success = true, isFavorite = isFavorite });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling favorite");
            return Json(new { success = false, message = "Error updating favorite" });
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetFavoriteStatus(int roomId)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { isFavorite = false });
            }

            var isFavorite = await _favoriteService.IsFavoriteAsync(userId, roomId);
            return Json(new { isFavorite = isFavorite });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorite status");
            return Json(new { isFavorite = false });
        }
    }

    public class ToggleFavoriteRequest
    {
        public int RoomId { get; set; }
    }
}

