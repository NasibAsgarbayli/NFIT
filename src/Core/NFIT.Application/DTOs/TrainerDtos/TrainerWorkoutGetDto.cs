using NFIT.Domain.Enums;

namespace NFIT.Application.DTOs.TrainerDtos;

public class TrainerWorkoutGetDto : TrainerWorkoutListItemDto
{
    public string Description { get; set; } = string.Empty;
    public int EstimatedDuration { get; set; }
    public MuscleGroup TargetMuscles { get; set; }
    public EquipmentType RequiredEquipment { get; set; }
    public string? PreviewVideoUrl { get; set; }
    public List<TrainerWorkoutLineDto> Lines { get; set; } = new();
}
