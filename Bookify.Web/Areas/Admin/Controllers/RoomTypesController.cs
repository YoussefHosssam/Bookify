using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bookify.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Bookify.Data.Interfaces;

namespace Bookify.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class RoomTypesController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RoomTypesController> _logger;

    public RoomTypesController(IUnitOfWork unitOfWork, ILogger<RoomTypesController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var roomTypes = await _unitOfWork.RoomTypes.GetAllAsync();
        return View(roomTypes);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoomType roomType)
    {
        if (ModelState.IsValid)
        {
            roomType.CreatedAt = DateTime.UtcNow;
            await _unitOfWork.RoomTypes.AddAsync(roomType);
            await _unitOfWork.CommitAsync();
            TempData["Success"] = "Room type created successfully.";
            return RedirectToAction(nameof(Index));
        }
        return View(roomType);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(id);
        if (roomType == null)
        {
            return NotFound();
        }
        return View(roomType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RoomType roomType)
    {
        if (id != roomType.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            _unitOfWork.RoomTypes.Update(roomType);
            await _unitOfWork.CommitAsync();
            TempData["Success"] = "Room type updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        return View(roomType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(id);
        if (roomType == null)
        {
            TempData["Error"] = "Room type not found.";
            return RedirectToAction(nameof(Index));
        }

        // Check if there are any rooms using this room type
        var roomsUsingType = await _unitOfWork.Rooms.GetAllAsync();
        var roomsCount = roomsUsingType.Count(r => r.RoomTypeId == id);
        
        if (roomsCount > 0)
        {
            TempData["Error"] = $"Cannot delete room type '{roomType.Name}' because it is being used by {roomsCount} room(s). Please delete or reassign the rooms first.";
            _logger.LogWarning("Attempted to delete room type {RoomTypeId} ({RoomTypeName}) which has {RoomsCount} associated rooms", 
                id, roomType.Name, roomsCount);
            return RedirectToAction(nameof(Index));
        }

        try
        {
            _unitOfWork.RoomTypes.Remove(roomType);
            await _unitOfWork.CommitAsync();
            TempData["Success"] = "Room type deleted successfully.";
            _logger.LogInformation("Room type {RoomTypeId} ({RoomTypeName}) deleted successfully", id, roomType.Name);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error deleting room type {RoomTypeId}", id);
            TempData["Error"] = "An error occurred while deleting the room type. It may be referenced by other records.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting room type {RoomTypeId}", id);
            TempData["Error"] = "An unexpected error occurred while deleting the room type.";
        }

        return RedirectToAction(nameof(Index));
    }
}

