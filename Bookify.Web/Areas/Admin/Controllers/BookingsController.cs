using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bookify.Core.Enums;
using Bookify.Data.Interfaces;

namespace Bookify.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BookingsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(IUnitOfWork unitOfWork, ILogger<BookingsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var bookings = await _unitOfWork.Bookings.GetAllAsync();
        // Load navigation properties
        var bookingsList = bookings.ToList();
        foreach (var booking in bookingsList)
        {
            if (booking.Room == null)
            {
                booking.Room = await _unitOfWork.Rooms.GetByIdAsync(booking.RoomId);
            }
            if (booking.RoomType == null)
            {
                booking.RoomType = await _unitOfWork.RoomTypes.GetByIdAsync(booking.RoomTypeId);
            }
        }
        return View(bookingsList);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var booking = await _unitOfWork.Bookings.GetByIdAsync(id);
        if (booking == null)
        {
            return NotFound();
        }
        // Load navigation properties
        if (booking.Room == null)
        {
            booking.Room = await _unitOfWork.Rooms.GetByIdAsync(booking.RoomId);
        }
        if (booking.RoomType == null)
        {
            booking.RoomType = await _unitOfWork.RoomTypes.GetByIdAsync(booking.RoomTypeId);
        }
        return View(booking);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var booking = await _unitOfWork.Bookings.GetByIdAsync(id);
        if (booking == null)
        {
            return NotFound();
        }

        booking.Status = BookingStatus.Cancelled.ToString();
        booking.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Bookings.Update(booking);
        await _unitOfWork.CommitAsync();

        TempData["Success"] = "Booking cancelled successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Export()
    {
        var bookings = await _unitOfWork.Bookings.GetAllAsync();
        
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("BookingNumber,UserEmail,RoomNumber,CheckIn,CheckOut,Nights,TotalAmount,Status,CreatedAt");

        foreach (var booking in bookings)
        {
            csv.AppendLine($"{booking.BookingNumber},{booking.UserId},{booking.RoomId},{booking.CheckIn:yyyy-MM-dd},{booking.CheckOut:yyyy-MM-dd},{booking.Nights},{booking.TotalAmount},{booking.Status},{booking.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"bookings_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}

