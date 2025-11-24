using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bookify.Data.Entities;

public class RoomImage
{
    public int Id { get; set; }

    public int RoomId { get; set; }

    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    // Navigation properties
    [ForeignKey(nameof(RoomId))]
    public Room Room { get; set; } = null!;
}

