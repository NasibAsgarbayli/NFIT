using FluentValidation;
using NFIT.Application.DTOs.DistrictDtos;

namespace NFIT.Application.Validations.DistrictValidations;

public class DistrictCreateValidator:AbstractValidator<DistrictCreateDto>
{
    public DistrictCreateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .Length(2, 100).WithMessage("Name length must be 2-100.");
    }
}
