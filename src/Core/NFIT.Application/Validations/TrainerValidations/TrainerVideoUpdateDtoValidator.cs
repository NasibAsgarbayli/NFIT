using FluentValidation;
using NFIT.Application.DTOs.TrainerDtos;

namespace NFIT.Application.Validations.TrainerValidations;

public class TrainerVideoUpdateDtoValidator : AbstractValidator<TrainerVideoUpdateDto>
{
    public TrainerVideoUpdateDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        Include(new TrainerVideoCreateDtoValidator());
    }
}
