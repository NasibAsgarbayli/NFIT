namespace NFIT.Application.DTOs.TrainerDtos;

public class TrainerFilterDto
{
    public string? Search { get; set; }                      // name/bio contains
    public string[]? Specializations { get; set; }
    public int? MinExperienceYears { get; set; }
    public bool? IsVerified { get; set; }
    public bool OnlyActive { get; set; } = true;

    public string? SortBy { get; set; } = "rating";          // rating|experience|name
    public bool Desc { get; set; } = true;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
