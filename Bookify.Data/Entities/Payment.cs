using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bookify.Core.Enums;

namespace Bookify.Data.Entities;

public class Payment
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    [MaxLength(50)]
    public string PaymentProvider { get; set; } = Core.Enums.PaymentProvider.Stripe.ToString();

    [MaxLength(200)]
    public string ProviderTransactionId { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "USD";

    [MaxLength(50)]
    public string Status { get; set; } = PaymentStatus.Pending.ToString();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; } = null!;
}

