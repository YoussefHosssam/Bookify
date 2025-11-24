using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bookify.Data.Entities;

public class Room
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string RoomNumber { get; set; } = string.Empty;

    public int RoomTypeId { get; set; }

    public int Floor { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(RoomTypeId))]
    public RoomType RoomType { get; set; } = null!;

    public ICollection<RoomImage> Images { get; set; } = new List<RoomImage>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

