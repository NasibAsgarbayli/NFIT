namespace NFIT.Application.DTOs.MembershipDtos;

public sealed class MembershipListItemDto
{
    public Guid Id { get; set; }
    public string PlanName { get; set; } 
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
}
