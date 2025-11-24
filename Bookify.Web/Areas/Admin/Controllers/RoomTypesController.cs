using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bookify.Data.Repositories;
using Bookify.Data.Entities;
using Microsoft.EntityFrameworkCore;

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
            return NotFound();
        }

        _unitOfWork.RoomTypes.Remove(roomType);
        await _unitOfWork.CommitAsync();
        TempData["Success"] = "Room type deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}

