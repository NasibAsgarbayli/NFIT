using FluentValidation;
using NFIT.Application.DTOs.TrainerDtos;

namespace NFIT.Application.Validations.TrainerValidations;

public class TrainerCreateDtoValidator : AbstractValidator<TrainerCreateDto>
{
    public TrainerCreateDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Bio).MaximumLength(1000);
        RuleFor(x => x.ExperienceYears).GreaterThanOrEqualTo(0);
        RuleForEach(x => x.Specializations).MaximumLength(50);
        RuleForEach(x => x.Certifications).MaximumLength(100);
        RuleFor(x => x.InstagramUrl).MaximumLength(300);
        RuleFor(x => x.YoutubeUrl).MaximumLength(300);
    }
}
