using FluentValidation;
using NFIT.Application.DTOs.SubscriptionPlanDtos;

namespace NFIT.Application.Validations.SubscriptionPlanCreateValidations;

public class SubscriptionPlanCreateValidator:AbstractValidator<SubscriptionPlanCreateDto>
{
    public SubscriptionPlanCreateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .Length(2, 100);

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description != null);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be >= 0.");
    }
}
