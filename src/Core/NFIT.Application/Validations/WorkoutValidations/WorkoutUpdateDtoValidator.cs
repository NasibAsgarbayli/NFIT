using FluentValidation;
using NFIT.Application.DTOs.WorkoutDtos;

namespace NFIT.Application.Validations.WorkoutValidations;

public class WorkoutUpdateDtoValidator : AbstractValidator<WorkoutUpdateDto>
{
    public WorkoutUpdateDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        Include(new WorkoutCreateDtoValidator());
    }
}
