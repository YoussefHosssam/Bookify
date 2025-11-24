using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bookify.Data.Repositories;
using Bookify.Data.Entities;

namespace Bookify.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class RoomsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RoomsController> _logger;
    private readonly IWebHostEnvironment _environment;

    public RoomsController(
        IUnitOfWork unitOfWork,
        ILogger<RoomsController> logger,
        IWebHostEnvironment environment)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        var rooms = await _unitOfWork.Rooms.GetAllAsync();
        // Load navigation properties
        var roomsList = rooms.ToList();
        foreach (var room in roomsList)
        {
            if (room.RoomType == null)
            {
                room.RoomType = await _unitOfWork.RoomTypes.GetByIdAsync(room.RoomTypeId);
            }
        }
        return View(roomsList);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.RoomTypes = await _unitOfWork.RoomTypes.GetAllAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Room room)
    {
        if (ModelState.IsValid)
        {
            room.CreatedAt = DateTime.UtcNow;
            await _unitOfWork.Rooms.AddAsync(room);
            await _unitOfWork.CommitAsync();
            TempData["Success"] = "Room created successfully.";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.RoomTypes = await _unitOfWork.RoomTypes.GetAllAsync();
        return View(room);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var room = await _unitOfWork.Rooms.GetWithImagesAsync(id);
        if (room == null)
        {
            return NotFound();
        }
        ViewBag.RoomTypes = await _unitOfWork.RoomTypes.GetAllAsync();
        
        // Load room images
        var roomImages = await _unitOfWork.RoomImages.GetAllAsync();
        ViewBag.RoomImages = roomImages.Where(ri => ri.RoomId == id).OrderBy(ri => ri.SortOrder).ToList();
        
        return View(room);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Room room)
    {
        if (id != room.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            _unitOfWork.Rooms.Update(room);
            await _unitOfWork.CommitAsync();
            TempData["Success"] = "Room updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.RoomTypes = await _unitOfWork.RoomTypes.GetAllAsync();
        return View(room);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var room = await _unitOfWork.Rooms.GetByIdAsync(id);
        if (room == null)
        {
            return NotFound();
        }

        _unitOfWork.Rooms.Remove(room);
        await _unitOfWork.CommitAsync();
        TempData["Success"] = "Room deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(int id, IFormFile file)
    {
        var room = await _unitOfWork.Rooms.GetByIdAsync(id);
        if (room == null)
        {
            return Json(new { success = false, message = "Room not found" });
        }

        if (file == null || file.Length == 0)
        {
            return Json(new { success = false, message = "No file provided" });
        }

        // Validate file size (5MB)
        if (file.Length > 5 * 1024 * 1024)
        {
            return Json(new { success = false, message = "File size must be less than 5MB" });
        }

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            return Json(new { success = false, message = "Only JPG, PNG, and GIF images are allowed" });
        }

        try
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "rooms");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var imageUrl = $"/images/rooms/{fileName}";
            var allImages = await _unitOfWork.RoomImages.GetAllAsync();
            var maxSortOrder = allImages
                .Where(r => r.RoomId == id)
                .DefaultIfEmpty()
                .Max(r => r?.SortOrder ?? 0);

            var roomImage = new RoomImage
            {
                RoomId = id,
                Url = imageUrl,
                SortOrder = maxSortOrder + 1
            };

            await _unitOfWork.RoomImages.AddAsync(roomImage);
            await _unitOfWork.CommitAsync();

            return Json(new { success = true, imageUrl, imageId = roomImage.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image for room {RoomId}", id);
            return Json(new { success = false, message = "Error uploading image: " + ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveImage(int imageId)
    {
        var image = await _unitOfWork.RoomImages.GetByIdAsync(imageId);
        if (image == null)
        {
            return NotFound();
        }

        // Delete physical file
        var filePath = Path.Combine(_environment.WebRootPath, image.Url.TrimStart('/'));
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }

        _unitOfWork.RoomImages.Remove(image);
        await _unitOfWork.CommitAsync();

        return Json(new { success = true });
    }
}

