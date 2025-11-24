using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bookify.Services.Interfaces;
using Stripe;
using System.Text;

namespace Bookify.Web.Controllers;

public class PaymentController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly IBookingService _bookingService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IPaymentService paymentService,
        IBookingService bookingService,
        ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _bookingService = bookingService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreatePaymentIntent(string bookingNumber, decimal amount, string currency)
    {
        try
        {
            var clientSecret = await _paymentService.CreatePaymentIntentAsync(amount, currency, bookingNumber);
            return Json(new { clientSecret });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent");
            return BadRequest(new { error = "Failed to create payment intent" });
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();

        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("Missing Stripe signature header");
            return BadRequest();
        }

        try
        {
            var isValid = await _paymentService.VerifyWebhookSignatureAsync(json, signature);
            if (!isValid)
            {
                _logger.LogWarning("Invalid Stripe webhook signature");
                return BadRequest();
            }

            await _paymentService.ProcessPaymentWebhookAsync(json);
            _logger.LogInformation("Stripe webhook processed successfully");
            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error processing webhook: {Message}", ex.Message);
            return BadRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return StatusCode(500);
        }
    }
}

