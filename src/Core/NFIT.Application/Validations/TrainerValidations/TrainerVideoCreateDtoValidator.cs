using FluentValidation;
using NFIT.Application.DTOs.TrainerDtos;

namespace NFIT.Application.Validations.TrainerValidations;

public class TrainerVideoCreateDtoValidator : AbstractValidator<TrainerVideoCreateDto>
{
    public TrainerVideoCreateDtoValidator()
    {
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Duration).GreaterThan(0);
        RuleFor(x => x.VideoUrl).MaximumLength(500);      // file upload edəcəksə boş ola bilər
        RuleFor(x => x.ThumbnailUrl).MaximumLength(500);
    }
}
