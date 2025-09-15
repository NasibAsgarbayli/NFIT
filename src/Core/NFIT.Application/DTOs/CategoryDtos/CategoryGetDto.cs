namespace NFIT.Application.DTOs.CategoryDtos;

public class CategoryGetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } 
    public string? Description { get; set; }
    public int GymCount { get; set; }
}
