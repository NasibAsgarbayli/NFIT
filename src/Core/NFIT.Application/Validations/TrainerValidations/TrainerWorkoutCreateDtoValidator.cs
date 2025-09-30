using FluentValidation;
using NFIT.Application.DTOs.TrainerDtos;

namespace NFIT.Application.Validations.TrainerValidations;

public class TrainerWorkoutCreateDtoValidator : AbstractValidator<TrainerWorkoutCreateDto>
{
    public TrainerWorkoutCreateDtoValidator()
    {
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.EstimatedDuration).GreaterThan(0);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ExerciseId).NotEmpty();
            line.RuleFor(l => l.Sets).GreaterThan(0);
            line.RuleFor(l => l.Reps).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.RestTimeSeconds).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.ThumbnailUrl).MaximumLength(500);
        RuleFor(x => x.PreviewVideoUrl).MaximumLength(500);
    }
}
