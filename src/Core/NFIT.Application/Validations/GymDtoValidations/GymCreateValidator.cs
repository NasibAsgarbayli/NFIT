using FluentValidation;
using NFIT.Application.DTOs.GymDtos;

namespace NFIT.Application.Validations.GymDtoValidations;

public class GymCreateValidator:AbstractValidator<GymCreateDto>
{
    public GymCreateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .Length(2, 100).WithMessage("Name must be 2-100 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(200);

        RuleFor(x => x.DistrictId)
            .NotEmpty().WithMessage("DistrictId is required.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m);

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[0-9\s\-()]{5,25}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Phone format is invalid.");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Website)
            .Must(IsValidUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.Website))
            .WithMessage("Website must be a valid absolute URL.");

        RuleFor(x => x.InstagramLink)
            .Must(IsValidUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.InstagramLink))
            .WithMessage("InstagramLink must be a valid absolute URL.");

        RuleForEach(x => x.CategoryIds)
            .Must(id => id != Guid.Empty)
            .WithMessage("CategoryIds contains an empty Guid.");

        RuleForEach(x => x.SubscriptionPlanIds)
            .Must(id => id != Guid.Empty)
            .WithMessage("SubscriptionPlanIds contains an empty Guid.");
    }

    private static bool IsValidUrl(string? url)
        => Uri.TryCreate(url, UriKind.Absolute, out _);
}
