using FluentValidation;
using NFIT.Application.DTOs.SubscriptionPlanDtos;
using NFIT.Application.Validations.SubscriptionPlanCreateValidations;

namespace NFIT.Application.Validations.SubscriptionPlanValidations;

public class SubscriptionPlanUpdateValidator:AbstractValidator<SubscriptionPlanUpdateDto>
{
    public SubscriptionPlanUpdateValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        Include(new SubscriptionPlanCreateValidator());
    }
}
