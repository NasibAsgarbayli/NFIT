namespace NFIT.Application.DTOs.TrainerDtos;

public class TrainerListItemDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public int TotalRatings { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; }
}
