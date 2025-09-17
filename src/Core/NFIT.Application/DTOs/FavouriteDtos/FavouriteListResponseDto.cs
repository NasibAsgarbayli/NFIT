namespace NFIT.Application.DTOs.FavouriteDtos;

public class FavouriteListResponseDto
{
    public int TotalCount { get; set; }
    public List<FavouriteListItemDto> Items { get; set; } = new();
}
