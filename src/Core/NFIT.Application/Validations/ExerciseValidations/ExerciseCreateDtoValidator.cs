using FluentValidation;
using NFIT.Application.DTOs.ExerciseDtos;

namespace NFIT.Application.Validations.ExerciseValidations;

public class ExerciseCreateDtoValidator:AbstractValidator<ExerciseCreateDto>
{
    public ExerciseCreateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.PrimaryMuscleGroup).IsInEnum();
        RuleFor(x => x.Difficulty).IsInEnum();
        RuleFor(x => x.Equipment).IsInEnum();
        RuleFor(x => x.VideoUrl).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.VideoUrl));
    }
}
