using Bookify.Core.ViewModels;

namespace Bookify.Services.Interfaces;

public interface IPaymentService
{
    Task<string> CreateCheckoutSessionAsync(CartViewModel cart, string bookingNumber, string userId, string? customerEmail = null);
    Task<string> CreatePaymentIntentAsync(decimal amount, string currency, string bookingNumber);
    Task<bool> VerifyWebhookSignatureAsync(string payload, string signature);
    Task ProcessPaymentWebhookAsync(string payload);
}

