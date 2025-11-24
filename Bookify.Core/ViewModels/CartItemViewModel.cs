namespace Bookify.Core.ViewModels;

public class CartItemViewModel
{
    public string CartItemId { get; set; } = Guid.NewGuid().ToString();
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public int RoomTypeId { get; set; }
    public decimal PricePerNight { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Nights { get; set; }
    public decimal SubTotal { get; set; }
    public string? ImageUrl { get; set; }
}

