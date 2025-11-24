using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bookify.Data.Entities;

public class RoomType
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int Capacity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BasePricePerNight { get; set; }

    public string? Amenities { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}

