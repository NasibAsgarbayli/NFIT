namespace NFIT.Application.DTOs.ReviewDtos;

public class ReviewCreateDto
{
    public Guid? GymId { get; set; }
    public Guid? TrainerId { get; set; }
    public Guid? SupplementId { get; set; }

    public string? Content { get; set; }
    public int Rating { get; set; }
}
