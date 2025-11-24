using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Bookify.Web.Models;
using Bookify.Services.Interfaces;

namespace Bookify.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IRoomService _roomService;

    public HomeController(ILogger<HomeController> logger, IRoomService roomService)
    {
        _logger = logger;
        _roomService = roomService;
    }

    public async Task<IActionResult> Index()
    {
        var featuredRooms = await _roomService.GetFeaturedRoomsAsync();
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
