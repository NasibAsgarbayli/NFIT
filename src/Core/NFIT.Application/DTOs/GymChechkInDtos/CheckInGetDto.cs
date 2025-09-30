namespace NFIT.Application.DTOs.GymChechkInDtos;

public class CheckInGetDto
{
    public Guid Id { get; set; }
    public Guid GymId { get; set; }
    public string GymName { get; set; } = default!;
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string Status { get; set; } = default!;
    public TimeSpan? Duration { get; set; }
}
