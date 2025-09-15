namespace NFIT.Application.DTOs.FavouriteDtos;

public class FavouriteListItemDto
{
    public Guid Id { get; set; }
    public string Type { get; set; }    // Gym Trainer Supplement
    public Guid EntityId { get; set; }
    public string Name { get; set; }   // göstərmək üçün ad
    public DateTime? AddedAt { get; set; }
}
