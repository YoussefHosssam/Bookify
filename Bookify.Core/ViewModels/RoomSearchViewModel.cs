namespace Bookify.Core.ViewModels;

public class RoomSearchViewModel
{
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public int? Adults { get; set; }
    public int? Children { get; set; }
    public int? TypeId { get; set; }
    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
    public string? SearchText { get; set; }
    public bool? FavoritesOnly { get; set; }
    public string? UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string SortBy { get; set; } = "popular";
}

