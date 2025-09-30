using FluentValidation;
using NFIT.Application.DTOs.WorkoutDtos;

namespace NFIT.Application.Validations.WorkoutValidations;

public class WorkoutCreateDtoValidator : AbstractValidator<WorkoutCreateDto>
{
    public WorkoutCreateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.EstimatedDuration).GreaterThan(0);
        RuleFor(x => x.Exercises).NotEmpty();
        RuleForEach(x => x.Exercises).ChildRules(ex =>
        {
            ex.RuleFor(e => e.ExerciseId).NotEmpty();
            ex.RuleFor(e => e.Sets).GreaterThan(0);
            ex.RuleFor(e => e.Reps).GreaterThanOrEqualTo(0);
            ex.RuleFor(e => e.RestTimeSeconds).GreaterThanOrEqualTo(0);
        });
    }
}
