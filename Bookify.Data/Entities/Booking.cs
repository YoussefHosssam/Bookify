using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bookify.Core.Enums;

namespace Bookify.Data.Entities;

public class Booking
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string BookingNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public int RoomId { get; set; }

    public int RoomTypeId { get; set; }

    [Column(TypeName = "date")]
    public DateTime CheckIn { get; set; }

    [Column(TypeName = "date")]
    public DateTime CheckOut { get; set; }

    public int Nights { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "USD";

    [MaxLength(50)]
    public string Status { get; set; } = BookingStatus.PendingPayment.ToString();

    [MaxLength(100)]
    public string? StripePaymentIntentId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(RoomId))]
    public Room Room { get; set; } = null!;

    [ForeignKey(nameof(RoomTypeId))]
    public RoomType RoomType { get; set; } = null!;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

