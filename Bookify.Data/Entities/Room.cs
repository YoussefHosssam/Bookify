using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bookify.Data.Entities;

public class Room
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Room number is required")]
    [MaxLength(50, ErrorMessage = "Room number cannot exceed 50 characters")]
    public string RoomNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Room type is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid room type")]
    public int RoomTypeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Floor must be greater than 0")]
    public int Floor { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(RoomTypeId))]
    public RoomType RoomType { get; set; } = null!;

    public ICollection<RoomImage> Images { get; set; } = new List<RoomImage>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

