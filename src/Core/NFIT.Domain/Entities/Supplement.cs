namespace NFIT.Domain.Entities;

public class Supplement
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? Brand { get; set; }
    public string? Flavor { get; set; }
    public decimal? Weight { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<OrderSupplement> Orders { get; set; }
    public ICollection<Favourite> Favourites { get; set; }
    public ICollection<Review> Reviews { get; set; }
    public ICollection<Image> Images { get; set; }

}
