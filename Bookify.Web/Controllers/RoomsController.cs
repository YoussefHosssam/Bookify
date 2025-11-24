using Microsoft.AspNetCore.Mvc;
using Bookify.Core.ViewModels;
using Bookify.Services.Interfaces;

namespace Bookify.Web.Controllers;

public class RoomsController : Controller
{
    private readonly IRoomService _roomService;
    private readonly ILogger<RoomsController> _logger;

    public RoomsController(IRoomService roomService, ILogger<RoomsController> logger)
    {
        _roomService = roomService;
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

        var result = await _roomService.SearchRoomsAsync(searchModel);
        ViewBag.RoomTypes = await _roomService.GetAllRoomTypesAsync();
        ViewBag.CheckIn = searchModel.CheckIn?.ToString("yyyy-MM-dd");
        ViewBag.CheckOut = searchModel.CheckOut?.ToString("yyyy-MM-dd");
        ViewBag.TypeId = searchModel.TypeId;
        ViewBag.Adults = searchModel.Adults;
        ViewBag.SortBy = searchModel.SortBy;
        
        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var room = await _roomService.GetRoomByIdAsync(id);
        if (room == null)
        {
            return NotFound();
        }

        return View(room);
    }
}

