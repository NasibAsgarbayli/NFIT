namespace NFIT.Application.DTOs.ReviewDtos;

public class ReviewGetDto
{
    public Guid Id { get; set; }
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public Guid? GymId { get; set; }
    public Guid? TrainerId { get; set; }
    public Guid? SupplementId { get; set; }

    public string? UserId { get; set; }
    public string? UserFullName { get; set; }

    public string? Content { get; set; }
    public int Rating { get; set; }
    public DateTime CreatedAt { get; set; }
}
