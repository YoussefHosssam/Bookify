using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bookify.Data.Entities;
using Bookify.Data.Interfaces;

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
    public async Task<IActionResult> Create([Bind("Id,RoomNumber,RoomTypeId,Floor,IsActive,CreatedAt")] Room room)
    {
        _logger.LogInformation("Create POST called. RoomNumber: {RoomNumber}, RoomTypeId: {RoomTypeId}, Floor: {Floor}", 
            room?.RoomNumber, room?.RoomTypeId, room?.Floor);

        // Remove navigation properties from ModelState validation (they're already excluded by [Bind], but just to be safe)
        ModelState.Remove(nameof(room.RoomType));
        ModelState.Remove(nameof(room.Images));
        ModelState.Remove(nameof(room.Bookings));

        // Log ModelState errors
        if (!ModelState.IsValid)
        {
            foreach (var error in ModelState)
            {
                foreach (var errorMessage in error.Value.Errors)
                {
                    _logger.LogWarning("ModelState Error - {Key}: {Message}", error.Key, errorMessage.ErrorMessage);
                }
            }
        }

        // Validate RoomTypeId
        if (room.RoomTypeId <= 0)
        {
            ModelState.AddModelError(nameof(room.RoomTypeId), "Please select a room type.");
        }
        else
        {
            // Verify room type exists
            var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(room.RoomTypeId);
            if (roomType == null)
            {
                ModelState.AddModelError(nameof(room.RoomTypeId), "Selected room type does not exist.");
            }
        }

        // Validate Floor
        if (room.Floor <= 0)
        {
            ModelState.AddModelError(nameof(room.Floor), "Floor must be greater than 0.");
        }

        // Validate RoomNumber
        if (string.IsNullOrWhiteSpace(room.RoomNumber))
        {
            ModelState.AddModelError(nameof(room.RoomNumber), "Room number is required.");
        }
        else
        {
            // Check if room number already exists
            var existingRooms = await _unitOfWork.Rooms.GetAllAsync();
            if (existingRooms.Any(r => r.RoomNumber.Equals(room.RoomNumber, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(room.RoomNumber), "A room with this number already exists.");
            }
        }

        if (ModelState.IsValid)
        {
            try
            {
                room.CreatedAt = DateTime.UtcNow;
                await _unitOfWork.Rooms.AddAsync(room);
                var saved = await _unitOfWork.CommitAsync();
                
                _logger.LogInformation("Room saved. Changes saved: {Saved}, RoomId: {RoomId}", saved, room.Id);
                
                TempData["Success"] = $"Room '{room.RoomNumber}' has been added successfully!";
                _logger.LogInformation("Room {RoomNumber} created successfully", room.RoomNumber);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating room. Exception: {Exception}", ex.ToString());
                ModelState.AddModelError(string.Empty, $"An error occurred while creating the room: {ex.Message}");
                ViewBag.RoomTypes = await _unitOfWork.RoomTypes.GetAllAsync();
                return View(room);
            }
        }
        
        // Log all validation errors
        _logger.LogWarning("ModelState is invalid. Errors: {Errors}", 
            string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
        
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

