namespace NFIT.Application.DTOs.ReviewDtos;

public class ReviewUpdateDto
{
    public Guid Id { get; set; }
    public string? Content { get; set; }
    public int Rating { get; set; }
}
