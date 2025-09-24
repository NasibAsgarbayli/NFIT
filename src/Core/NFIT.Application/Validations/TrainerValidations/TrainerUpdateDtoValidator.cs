using FluentValidation;
using NFIT.Application.DTOs.TrainerDtos;

namespace NFIT.Application.Validations.TrainerValidations;

public class TrainerUpdateDtoValidator : AbstractValidator<TrainerUpdateDto>
{
    public TrainerUpdateDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        Include(new TrainerCreateDtoValidator());
    }

}
