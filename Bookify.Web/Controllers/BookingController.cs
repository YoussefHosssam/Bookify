using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bookify.Core.ViewModels;
using Bookify.Services.Interfaces;
using Stripe;
using Stripe.Checkout;
using System.Text.Json;

namespace Bookify.Web.Controllers;

[Authorize]
public class BookingController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BookingController> _logger;
    private const string CartSessionKeyPrefix = "Cart_";

    public BookingController(
        IBookingService bookingService,
        IPaymentService paymentService,
        IConfiguration configuration,
        ILogger<BookingController> logger)
    {
        _bookingService = bookingService;
        _paymentService = paymentService;
        _configuration = configuration;
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

    private CartViewModel? GetCart()
    {
        var cartKey = GetCartSessionKey();
        var cartJson = HttpContext.Session.GetString(cartKey);
        if (string.IsNullOrEmpty(cartJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<CartViewModel>(cartJson);
    }

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var cart = GetCart();
        if (cart == null || !cart.Items.Any())
        {
            return RedirectToAction("ViewCart", "Cart");
        }

        // Validate availability
        var isAvailable = await _bookingService.ValidateCartAvailabilityAsync(cart);
        if (!isAvailable)
        {
            TempData["Error"] = "One or more rooms are no longer available. Please update your cart.";
            return RedirectToAction("ViewCart", "Cart");
        }

        var checkoutModel = new CheckoutViewModel
        {
            Cart = cart,
            Email = User.Identity?.Name ?? string.Empty,
            StripePublishableKey = _configuration["Stripe:PublishableKey"] ?? string.Empty
        };

        return View(checkoutModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(CheckoutViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Checkout", model);
        }

        var cart = GetCart();
        if (cart == null || !cart.Items.Any())
        {
            return RedirectToAction("ViewCart", "Cart");
        }

        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var bookings = await _bookingService.CreateBookingsAsync(userId, cart);

            // Get user email
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
                ?? User.Identity?.Name 
                ?? model.Email;

            // Get all booking numbers
            var bookingNumbers = bookings.Select(b => b.BookingNumber).ToList();

            // Create Stripe Checkout Session
            var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
                cart,
                bookingNumbers,
                userId,
                userEmail);

            if (string.IsNullOrEmpty(checkoutUrl))
            {
                throw new InvalidOperationException("Failed to create checkout session");
            }

            // Clear cart
            var cartKey = GetCartSessionKey();
            HttpContext.Session.Remove(cartKey);

            return Json(new
            {
                success = true,
                bookingNumbers = bookingNumbers,
                bookingCount = bookingNumbers.Count,
                checkoutUrl = checkoutUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking");
            return Json(new
            {
                success = false,
                message = "An error occurred while processing your booking. Please try again."
            });
        }
    }

    [HttpGet]
    [Route("/success.html")]
    [Route("/booking/success")]
    public async Task<IActionResult> Success(string session_id)
    {
        if (string.IsNullOrEmpty(session_id))
        {
            TempData["Error"] = "Invalid payment session. Please contact support.";
            return RedirectToAction("UserBookings");
        }

        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Please log in to view your booking.";
                return RedirectToAction("Index", "Home");
            }

            // Verify and update booking status from Stripe session
            var stripeSecretKey = _configuration["Stripe:ApiKey"] ?? _configuration["Stripe:SecretKey"];
            if (string.IsNullOrEmpty(stripeSecretKey))
            {
                _logger.LogError("Stripe API key not configured");
                TempData["Error"] = "Payment verification failed. Please contact support.";
                return RedirectToAction("UserBookings");
            }

            // Use PaymentService to verify session and update booking
            Stripe.StripeConfiguration.ApiKey = stripeSecretKey;
            var sessionService = new Stripe.Checkout.SessionService();
            var session = await sessionService.GetAsync(session_id);

            if (session == null)
            {
                TempData["Error"] = "Payment session not found.";
                return RedirectToAction("UserBookings");
            }

            // Get booking numbers from metadata (support both old and new format)
            string? bookingNumbersString = null;
            if (session.Metadata != null)
            {
                if (session.Metadata.ContainsKey("bookingNumbers"))
                {
                    bookingNumbersString = session.Metadata["bookingNumbers"];
                }
                else if (session.Metadata.ContainsKey("bookingNumber"))
                {
                    // Backward compatibility with old format
                    bookingNumbersString = session.Metadata["bookingNumber"];
                }
            }

            if (string.IsNullOrEmpty(bookingNumbersString))
            {
                TempData["Error"] = "Booking information not found in payment session.";
                return RedirectToAction("UserBookings");
            }

            // Split booking numbers
            var bookingNumbers = bookingNumbersString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(bn => bn.Trim())
                .ToList();

            // Verify all bookings belong to the current user
            var bookings = new List<Bookify.Core.DTOs.BookingDto>();
            foreach (var bookingNumber in bookingNumbers)
            {
                var booking = await _bookingService.GetBookingByNumberAsync(bookingNumber);
                if (booking == null || booking.UserId != userId)
                {
                    TempData["Error"] = $"Booking {bookingNumber} not found or access denied.";
                    return RedirectToAction("UserBookings");
                }
                bookings.Add(booking);
            }

            // Update bookings if payment was successful
            if (session.PaymentStatus == "paid")
            {
                var pendingBookings = bookings.Where(b => b.Status == Bookify.Core.Enums.BookingStatus.PendingPayment).ToList();
                
                if (pendingBookings.Any())
                {
                    // Confirm all pending bookings
                    var result = await _bookingService.ConfirmPaymentsAsync(
                        pendingBookings.Select(b => b.BookingNumber), 
                        session_id);
                    
                    if (result)
                    {
                        if (bookings.Count == 1)
                        {
                            TempData["Success"] = $"Payment successful! Your booking {bookings[0].BookingNumber} has been confirmed. You can view it in My Bookings.";
                        }
                        else
                        {
                            TempData["Success"] = $"Payment successful! Your {bookings.Count} bookings have been confirmed. Booking numbers: {string.Join(", ", bookings.Select(b => b.BookingNumber))}";
                        }
                        _logger.LogInformation("Bookings {BookingNumbers} confirmed via success page for user {UserId}", 
                            string.Join(", ", bookings.Select(b => b.BookingNumber)), userId);
                    }
                    else
                    {
                        TempData["Info"] = "Payment was successful, but there was an issue updating some bookings. Please contact support with booking numbers: " + string.Join(", ", bookings.Select(b => b.BookingNumber));
                    }
                }
                else
                {
                    // All bookings already confirmed
                    if (bookings.Count == 1)
                    {
                        TempData["Success"] = $"Your booking {bookings[0].BookingNumber} is already confirmed.";
                    }
                    else
                    {
                        TempData["Success"] = $"All your {bookings.Count} bookings are already confirmed.";
                    }
                }
            }
            else
            {
                TempData["Info"] = $"Payment status: {session.PaymentStatus}. Your bookings are being processed.";
            }
        }
        catch (Stripe.StripeException ex)
        {
            _logger.LogError(ex, "Stripe error verifying session {SessionId}", session_id);
            TempData["Error"] = "Error verifying payment. Please contact support with session ID: " + session_id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing success page for session {SessionId}", session_id);
            TempData["Error"] = "An error occurred while processing your payment. Please contact support.";
        }

        return RedirectToAction("UserBookings");
    }

    [HttpGet]
    public IActionResult Cancel()
    {
        TempData["Error"] = "Payment was cancelled. Your booking is still pending.";
        return RedirectToAction("UserBookings");
    }

    [HttpGet]
    public async Task<IActionResult> UserBookings()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var bookings = await _bookingService.GetUserBookingsAsync(userId);
        return View(bookings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Get booking to check CheckOut date
        var bookings = await _bookingService.GetUserBookingsAsync(userId);
        var booking = bookings.FirstOrDefault(b => b.Id == id);
        
        if (booking != null && booking.CheckOut < DateTime.Today)
        {
            TempData["Error"] = "Cannot cancel booking. The check-out date has passed. This booking has been marked as completed.";
            return RedirectToAction("UserBookings");
        }

        var result = await _bookingService.CancelBookingAsync(id, userId);
        if (result)
        {
            TempData["Success"] = "Booking cancelled successfully.";
        }
        else
        {
            TempData["Error"] = "Unable to cancel booking. The booking may have already been completed or cancelled.";
        }

        return RedirectToAction("UserBookings");
    }
}

