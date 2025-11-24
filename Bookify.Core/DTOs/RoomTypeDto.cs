namespace Bookify.Core.DTOs;

public class RoomTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Capacity { get; set; }
    public decimal BasePricePerNight { get; set; }
    public string? Amenities { get; set; }
    public DateTime CreatedAt { get; set; }
}

