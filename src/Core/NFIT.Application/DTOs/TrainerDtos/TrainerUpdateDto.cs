namespace NFIT.Application.DTOs.TrainerDtos;

public class TrainerUpdateDto : TrainerCreateDto
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsVerified { get; set; } = false;
}
