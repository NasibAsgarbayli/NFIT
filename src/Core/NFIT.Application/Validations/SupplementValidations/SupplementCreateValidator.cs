using FluentValidation;
using NFIT.Application.DTOs.SupplementDtos;

namespace NFIT.Application.Validations.SupplementValidations;

public class SupplementCreateValidator:AbstractValidator<SupplementCreateDto>
{
    public SupplementCreateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().Length(2, 150);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
