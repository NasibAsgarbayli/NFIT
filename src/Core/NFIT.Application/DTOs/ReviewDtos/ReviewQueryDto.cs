namespace NFIT.Application.DTOs.ReviewDtos;

public class ReviewQueryDto
{
    public Guid? GymId { get; set; }
    public Guid? TrainerId { get; set; }
    public Guid? SupplementId { get; set; }

    // Pagination (opsional)
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
