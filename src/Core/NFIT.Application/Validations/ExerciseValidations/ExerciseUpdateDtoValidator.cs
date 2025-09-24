using FluentValidation;
using NFIT.Application.DTOs.ExerciseDtos;

namespace NFIT.Application.Validations.ExerciseValidations;

public class ExerciseUpdateDtoValidator : AbstractValidator<ExerciseUpdateDto>
{
    public ExerciseUpdateDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        Include(new ExerciseCreateDtoValidator());
    }
}
