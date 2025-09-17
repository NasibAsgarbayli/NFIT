using FluentValidation;
using NFIT.Application.DTOs.DistrictDtos;

namespace NFIT.Application.Validations.DistrictValidations;

public class DistrictUpdateValidator:AbstractValidator<DistrictUpdateDto>
{
    public DistrictUpdateValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .Length(2, 100);
    }
}
