namespace NFIT.Application.DTOs.DistrictDtos;

public class DistrictGetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } 
    public bool IsActive { get; set; }
    public string City { get; set; } = "Baku";
    public int GymCount { get; set; }
}
