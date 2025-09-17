namespace NFIT.Application.DTOs.SupplementDtos;

public class SupplementGetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } 
    public string Description { get; set; } 
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }          
    public string? Brand { get; set; }
    public string? Flavor { get; set; }
    public decimal? Weight { get; set; }
    public bool IsActive { get; set; }
    public int FavouriteCount { get; set; }
}
