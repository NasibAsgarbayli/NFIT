using FluentValidation;
using NFIT.Application.DTOs.GymDtos;

namespace NFIT.Application.Validations.GymDtoValidations;

public class GymUpdateValidator:AbstractValidator<GymUpdateDto>
{
    public GymUpdateValidator()
    {
        Include(new GymCreateValidator());
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");
    }

}
