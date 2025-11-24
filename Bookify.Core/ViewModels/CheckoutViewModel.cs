namespace Bookify.Core.ViewModels;

public class CheckoutViewModel
{
    public CartViewModel Cart { get; set; } = new();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string StripePublishableKey { get; set; } = string.Empty;
}

