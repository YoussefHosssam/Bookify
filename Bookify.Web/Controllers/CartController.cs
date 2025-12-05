using Microsoft.AspNetCore.Mvc;
using Bookify.Core.ViewModels;
using Bookify.Services.Interfaces;
using System.Text.Json;

namespace Bookify.Web.Controllers;

public class CartController : Controller
{
    private readonly IRoomService _roomService;
    private readonly ILogger<CartController> _logger;
    private const string CartSessionKeyPrefix = "Cart_";

    public CartController(IRoomService roomService, ILogger<CartController> logger)
    {
        _roomService = roomService;
        _logger = logger;
    }

    private string GetCartSessionKey()
    {
        // Use userId if authenticated, otherwise use session ID
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"{CartSessionKeyPrefix}User_{userId}";
        }
        
        // For anonymous users, use session ID
        var sessionId = HttpContext.Session.Id;
        return $"{CartSessionKeyPrefix}Session_{sessionId}";
    }

    private CartViewModel GetCart()
    {
        var cartKey = GetCartSessionKey();
        var cartJson = HttpContext.Session.GetString(cartKey);
        if (string.IsNullOrEmpty(cartJson))
        {
            return new CartViewModel();
        }

        return JsonSerializer.Deserialize<CartViewModel>(cartJson) ?? new CartViewModel();
    }

    private void SaveCart(CartViewModel cart)
    {
        var cartKey = GetCartSessionKey();
        var cartJson = JsonSerializer.Serialize(cart);
        HttpContext.Session.SetString(cartKey, cartJson);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        if (request == null || request.CheckIn >= request.CheckOut || request.CheckIn < DateTime.Today)
        {
            return BadRequest(new { success = false, message = "Invalid dates" });
        }

        var room = await _roomService.GetRoomByIdAsync(request.RoomId);
        if (room == null)
        {
            return NotFound(new { success = false, message = "Room not found" });
        }

        var cart = GetCart();
        var nights = (int)(request.CheckOut - request.CheckIn).TotalDays;
        var subTotal = room.BasePricePerNight * nights;

        var cartItem = new CartItemViewModel
        {
            RoomId = room.Id,
            RoomNumber = room.RoomNumber,
            RoomTypeName = room.RoomTypeName,
            RoomTypeId = room.RoomTypeId,
            PricePerNight = room.BasePricePerNight,
            CheckIn = request.CheckIn,
            CheckOut = request.CheckOut,
            Nights = nights,
            SubTotal = subTotal,
            ImageUrl = room.ImageUrls.FirstOrDefault()
        };

        cart.Items.Add(cartItem);
        cart.TotalAmount = cart.Items.Sum(i => i.SubTotal);
        SaveCart(cart);

        return Json(new { success = true, cartItemCount = cart.Items.Count });
    }

    public class AddToCartRequest
    {
        public int RoomId { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult RemoveFromCart([FromBody] RemoveFromCartRequest request)
    {
        var cart = GetCart();
        cart.Items.RemoveAll(i => i.CartItemId == request.CartItemId);
        cart.TotalAmount = cart.Items.Sum(i => i.SubTotal);
        SaveCart(cart);

        return Json(new { success = true, cartItemCount = cart.Items.Count });
    }

    public class RemoveFromCartRequest
    {
        public string CartItemId { get; set; } = string.Empty;
    }

    [HttpGet]
    public IActionResult ViewCart()
    {
        var cart = GetCart();
        return View(cart);
    }

    [HttpGet]
    public IActionResult GetCartCount()
    {
        var cart = GetCart();
        return Json(new { count = cart.Items.Count });
    }

    [HttpPost]
    public IActionResult UpdateCart(string cartItemId, DateTime checkIn, DateTime checkOut)
    {
        if (checkIn >= checkOut || checkIn < DateTime.Today)
        {
            return BadRequest("Invalid dates");
        }

        var cart = GetCart();
        var item = cart.Items.FirstOrDefault(i => i.CartItemId == cartItemId);
        if (item == null)
        {
            return NotFound();
        }

        item.CheckIn = checkIn;
        item.CheckOut = checkOut;
        item.Nights = (int)(checkOut - checkIn).TotalDays;
        item.SubTotal = item.PricePerNight * item.Nights;

        cart.TotalAmount = cart.Items.Sum(i => i.SubTotal);
        SaveCart(cart);

        return Json(new { success = true, totalAmount = cart.TotalAmount });
    }
}

