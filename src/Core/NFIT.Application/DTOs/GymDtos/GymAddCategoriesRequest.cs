namespace NFIT.Application.DTOs.GymDtos;

public class GymAddCategoriesRequest
{
    public List<Guid> CategoryIds { get; set; } = new();
}
