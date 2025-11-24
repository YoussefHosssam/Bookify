using Bookify.Core.Enums;
using Bookify.Core.ViewModels;
using Bookify.Data.Entities;
using Bookify.Data.Repositories;
using Bookify.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace Bookify.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly string _stripeSecretKey;
    private readonly string _webhookSecret;
    private readonly string _successUrl;
    private readonly string _cancelUrl;

    public PaymentService(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _stripeSecretKey = _configuration["Stripe:ApiKey"] ?? _configuration["Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe ApiKey not configured");
        _webhookSecret = _configuration["Stripe:WebhookSecret"] ?? string.Empty;
        _successUrl = _configuration["Stripe:SuccessUrl"] ?? "https://localhost:7198/success.html?session_id={CHECKOUT_SESSION_ID}";
        _cancelUrl = _configuration["Stripe:CancelUrl"] ?? "https://localhost:7198/booking/cancel";
        StripeConfiguration.ApiKey = _stripeSecretKey;
    }

    public async Task<string> CreateCheckoutSessionAsync(CartViewModel cart, string bookingNumber, string userId, string? customerEmail = null)
    {
        var lineItems = new List<SessionLineItemOptions>();

        foreach (var item in cart.Items)
        {
            var room = await _unitOfWork.Rooms.GetWithImagesAsync(item.RoomId);
            if (room == null) continue;

            lineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = cart.Currency.ToLower(),
                    UnitAmount = (long)(item.PricePerNight * 100), // Convert to cents
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"{room.RoomNumber} - {room.RoomType.Name}",
                        Description = $"Check-in: {item.CheckIn:yyyy-MM-dd}, Check-out: {item.CheckOut:yyyy-MM-dd}"
                    }
                },
                Quantity = item.Nights
            });
        }

        var options = new SessionCreateOptions
        {
            SuccessUrl = _successUrl,
            CancelUrl = _cancelUrl,
            LineItems = lineItems,
            Mode = "payment",
            Metadata = new Dictionary<string, string>
            {
                { "bookingNumber", bookingNumber },
                { "userId", userId }
            }
        };

        if (!string.IsNullOrEmpty(customerEmail))
        {
            options.CustomerEmail = customerEmail;
        }

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return session.Url ?? string.Empty;
    }

    public async Task<string> CreatePaymentIntentAsync(decimal amount, string currency, string bookingNumber)
    {
        // Legacy method - kept for backward compatibility
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100),
            Currency = currency.ToLower(),
            Metadata = new Dictionary<string, string>
            {
                { "bookingNumber", bookingNumber }
            },
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true
            }
        };

        var service = new PaymentIntentService();
        var paymentIntent = await service.CreateAsync(options);

        return paymentIntent.ClientSecret;
    }

    public async Task<bool> VerifyWebhookSignatureAsync(string payload, string signature)
    {
        if (string.IsNullOrEmpty(_webhookSecret))
            return false;

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                payload,
                signature,
                _webhookSecret
            );
            return stripeEvent != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task ProcessPaymentWebhookAsync(string payload)
    {
        var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(payload);
        var eventType = json.GetProperty("type").GetString();

        if (eventType == "checkout.session.completed")
        {
            var session = json.GetProperty("data").GetProperty("object");
            var sessionId = session.GetProperty("id").GetString();
            var paymentStatus = session.GetProperty("payment_status").GetString();
            
            // Get booking number from metadata
            string? bookingNumber = null;
            if (session.TryGetProperty("metadata", out var metadata))
            {
                if (metadata.TryGetProperty("bookingNumber", out var bookingNumberProp))
                {
                    bookingNumber = bookingNumberProp.GetString();
                }
            }

            if (!string.IsNullOrEmpty(bookingNumber) && paymentStatus == "paid")
            {
                var booking = await _unitOfWork.Bookings.GetByBookingNumberAsync(bookingNumber);
                if (booking != null)
                {
                    // Update booking status to Confirmed
                    booking.Status = Core.Enums.BookingStatus.Confirmed.ToString();
                    booking.StripePaymentIntentId = sessionId; // Store session ID
                    booking.UpdatedAt = DateTime.UtcNow;

                    // Check if payment record already exists
                    var existingPayment = (await _unitOfWork.Payments.GetAllAsync())
                        .FirstOrDefault(p => p.ProviderTransactionId == sessionId);

                    if (existingPayment == null)
                    {
                        // Create payment record
                        var payment = new Payment
                        {
                            BookingId = booking.Id,
                            PaymentProvider = PaymentProvider.Stripe.ToString(),
                            ProviderTransactionId = sessionId ?? string.Empty,
                            Amount = booking.TotalAmount,
                            Currency = booking.Currency,
                            Status = PaymentStatus.Succeeded.ToString(),
                            CreatedAt = DateTime.UtcNow
                        };

                        await _unitOfWork.Payments.AddAsync(payment);
                    }
                    else
                    {
                        // Update existing payment
                        existingPayment.Status = PaymentStatus.Succeeded.ToString();
                        _unitOfWork.Payments.Update(existingPayment);
                    }

                    _unitOfWork.Bookings.Update(booking);
                    await _unitOfWork.CommitAsync();
                }
            }
        }
        else if (eventType == "payment_intent.succeeded")
        {
            // Legacy support for payment intents
            var paymentIntent = json.GetProperty("data").GetProperty("object");
            var bookingNumber = paymentIntent.GetProperty("metadata")
                .GetProperty("bookingNumber").GetString();

            if (!string.IsNullOrEmpty(bookingNumber))
            {
                var booking = await _unitOfWork.Bookings.GetByBookingNumberAsync(bookingNumber);
                if (booking != null && booking.Status == Core.Enums.BookingStatus.PendingPayment.ToString())
                {
                    booking.Status = Core.Enums.BookingStatus.Confirmed.ToString();
                    booking.StripePaymentIntentId = paymentIntent.GetProperty("id").GetString();
                    booking.UpdatedAt = DateTime.UtcNow;

                    var payment = new Payment
                    {
                        BookingId = booking.Id,
                        PaymentProvider = PaymentProvider.Stripe.ToString(),
                        ProviderTransactionId = paymentIntent.GetProperty("id").GetString() ?? string.Empty,
                        Amount = booking.TotalAmount,
                        Currency = booking.Currency,
                        Status = PaymentStatus.Succeeded.ToString(),
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.Payments.AddAsync(payment);
                    _unitOfWork.Bookings.Update(booking);
                    await _unitOfWork.CommitAsync();
                }
            }
        }
    }
}

