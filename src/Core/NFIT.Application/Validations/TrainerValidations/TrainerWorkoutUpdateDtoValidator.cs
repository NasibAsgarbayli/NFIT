using FluentValidation;
using NFIT.Application.DTOs.TrainerDtos;

namespace NFIT.Application.Validations.TrainerValidations;

public class TrainerWorkoutUpdateDtoValidator : AbstractValidator<TrainerWorkoutUpdateDto>
{
    public TrainerWorkoutUpdateDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        Include(new TrainerWorkoutCreateDtoValidator());
    }
}
