using FluentValidation;
using NFIT.Application.DTOs.ReviewDtos;

namespace NFIT.Application.Validations.ReviewValidations;

public class ReviewUpdateDtoValidator:AbstractValidator<ReviewUpdateDto>
{
    public ReviewUpdateDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Content).MaximumLength(2000);
    }
}
