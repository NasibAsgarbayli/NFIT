using FluentValidation;
using NFIT.Application.DTOs.ReviewDtos;

namespace NFIT.Application.Validations.ReviewValidations;

public class ReviewCreateDtoValidator:AbstractValidator<ReviewCreateDto>
{
    public ReviewCreateDtoValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Content).MaximumLength(2000);
        RuleFor(x => new { x.GymId, x.TrainerId, x.SupplementId })
            .Must(t =>
            {
                var c = 0;
                if (t.GymId.HasValue) c++;
                if (t.TrainerId.HasValue) c++;
                if (t.SupplementId.HasValue) c++;
                return c == 1;
            })
            .WithMessage("Exactly one of GymId, TrainerId or SupplementId must be provided.");
    }
}
