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
    private const string CartSessionKey = "Cart";

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

    private CartViewModel? GetCart()
    {
        var cartJson = HttpContext.Session.GetString(CartSessionKey);
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

            var booking = await _bookingService.CreateBookingAsync(userId, cart);

            // Get user email
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
                ?? User.Identity?.Name 
                ?? model.Email;

            // Create Stripe Checkout Session
            var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
                cart,
                booking.BookingNumber,
                userId,
                userEmail);

            if (string.IsNullOrEmpty(checkoutUrl))
            {
                throw new InvalidOperationException("Failed to create checkout session");
            }

            // Clear cart
            HttpContext.Session.Remove(CartSessionKey);

            return Json(new
            {
                success = true,
                bookingNumber = booking.BookingNumber,
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

            // Get booking number from metadata
            string? bookingNumber = null;
            if (session.Metadata != null && session.Metadata.ContainsKey("bookingNumber"))
            {
                bookingNumber = session.Metadata["bookingNumber"];
            }

            if (string.IsNullOrEmpty(bookingNumber))
            {
                TempData["Error"] = "Booking information not found in payment session.";
                return RedirectToAction("UserBookings");
            }

            // Verify this booking belongs to the current user
            var booking = await _bookingService.GetBookingByNumberAsync(bookingNumber);
            if (booking == null || booking.UserId != userId)
            {
                TempData["Error"] = "Booking not found or access denied.";
                return RedirectToAction("UserBookings");
            }

            // Update booking if payment was successful
            if (session.PaymentStatus == "paid" && booking.Status == Bookify.Core.Enums.BookingStatus.PendingPayment)
            {
                // Update booking status
                var result = await _bookingService.ConfirmPaymentAsync(bookingNumber, session_id);
                if (result)
                {
                    TempData["Success"] = $"Payment successful! Your booking {bookingNumber} has been confirmed. You can view it in My Bookings.";
                    _logger.LogInformation("Booking {BookingNumber} confirmed via success page for user {UserId}", bookingNumber, userId);
                }
                else
                {
                    TempData["Info"] = "Payment was successful, but there was an issue updating the booking. Please contact support with booking number: " + bookingNumber;
                }
            }
            else if (booking.Status == Bookify.Core.Enums.BookingStatus.Confirmed)
            {
                TempData["Success"] = $"Your booking {bookingNumber} is already confirmed.";
            }
            else
            {
                TempData["Info"] = $"Payment status: {session.PaymentStatus}. Your booking is being processed.";
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

        var result = await _bookingService.CancelBookingAsync(id, userId);
        if (result)
        {
            TempData["Success"] = "Booking cancelled successfully.";
        }
        else
        {
            TempData["Error"] = "Unable to cancel booking.";
        }

        return RedirectToAction("UserBookings");
    }
}

