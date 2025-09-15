using NFIT.Application.DTOs.GymDtos;

namespace NFIT.Application.DTOs.CategoryDtos;

public class CategoryWithGymsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int GymCount { get; set; }
    public List<GymListItemDto> Gyms { get; set; } = new();
}
