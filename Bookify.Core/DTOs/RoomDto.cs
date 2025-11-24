namespace Bookify.Core.DTOs;

public class RoomDto
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal BasePricePerNight { get; set; }
    public int Floor { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public string? Amenities { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}

