using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bookify.Data.Repositories;

namespace Bookify.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        var bookings = await _unitOfWork.Bookings.GetAllAsync();
        var rooms = await _unitOfWork.Rooms.GetAllAsync();
        var roomTypes = await _unitOfWork.RoomTypes.GetAllAsync();

        var stats = new
        {
            TotalBookings = bookings.Count(),
            TotalRooms = rooms.Count(),
            TotalRoomTypes = roomTypes.Count(),
            ActiveRooms = rooms.Count(r => r.IsActive),
            ConfirmedBookings = bookings.Count(b => b.Status == "Confirmed"),
            PendingBookings = bookings.Count(b => b.Status == "PendingPayment"),
            TotalRevenue = bookings.Where(b => b.Status == "Confirmed").Sum(b => (decimal?)b.TotalAmount) ?? 0
        };

        var recentBookings = bookings
            .OrderByDescending(b => b.CreatedAt)
            .Take(5)
            .ToList();

        var statusBreakdown = bookings
            .GroupBy(b => b.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
                Percentage = stats.TotalBookings == 0 ? 0 : Math.Round((double)g.Count() / stats.TotalBookings * 100, 1)
            })
            .OrderByDescending(s => s.Count)
            .ToList();

        var occupancyRate = stats.TotalRooms == 0 ? 0 : Math.Round((double)stats.ActiveRooms / stats.TotalRooms * 100, 1);

        ViewBag.Stats = stats;
        ViewBag.RecentBookings = recentBookings;
        ViewBag.StatusBreakdown = statusBreakdown;
        ViewBag.OccupancyRate = occupancyRate;
        return View();
    }
}

