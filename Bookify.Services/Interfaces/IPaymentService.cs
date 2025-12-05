using Bookify.Core.ViewModels;

namespace Bookify.Services.Interfaces;

public interface IPaymentService
{
    Task<string> CreateCheckoutSessionAsync(CartViewModel cart, IEnumerable<string> bookingNumbers, string userId, string? customerEmail = null);
    Task<bool> VerifyWebhookSignatureAsync(string payload, string signature);
    Task ProcessPaymentWebhookAsync(string payload);
}

