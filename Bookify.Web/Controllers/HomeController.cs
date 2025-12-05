using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Bookify.Web.Models;
using Bookify.Services.Interfaces;

namespace Bookify.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IRoomService _roomService;
    private readonly IRoomFeedbackService _feedbackService;

    public HomeController(ILogger<HomeController> logger, IRoomService roomService, IRoomFeedbackService feedbackService)
    {
        _logger = logger;
        _roomService = roomService;
        _feedbackService = feedbackService;
    }

    public async Task<IActionResult> Index()
    {
        var featuredRooms = await _roomService.GetFeaturedRoomsAsync();
        var latestFeedbacks = await _feedbackService.GetLatestFeedbacksAsync(3);
        
        // Populate user names for feedbacks
        var userIds = latestFeedbacks.Select(f => f.UserId).Distinct().ToList();
        var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
        var users = new Dictionary<string, IdentityUser?>();
        foreach (var userId in userIds)
        {
            users[userId] = await userManager.FindByIdAsync(userId);
        }
        foreach (var feedback in latestFeedbacks)
        {
            if (users.ContainsKey(feedback.UserId))
            {
                feedback.UserName = users[feedback.UserId]?.UserName ?? "Anonymous";
                feedback.UserEmail = users[feedback.UserId]?.Email ?? "";
            }
        }
        
        ViewBag.LatestFeedbacks = latestFeedbacks;
        return View(featuredRooms);
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    public IActionResult Facilities()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
