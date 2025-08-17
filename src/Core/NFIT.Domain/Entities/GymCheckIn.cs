using NFIT.Domain.Enums;

namespace NFIT.Domain.Entities;

public class GymCheckIn:BaseEntity
{
    public string UserId { get; set; } = null!;
    public AppUser User { get; set; } = null!;

    public Guid GymId { get; set; }
    public Gym Gym { get; set; } = null!;

    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public CheckInStatus Status { get; set; } = CheckInStatus.Active;
    public string? Notes { get; set; }

    // Calculated property
    public TimeSpan? Duration => CheckOutTime.HasValue ? CheckOutTime - CheckInTime : null;
}
