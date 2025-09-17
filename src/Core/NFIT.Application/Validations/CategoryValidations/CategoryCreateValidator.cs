using FluentValidation;
using NFIT.Application.DTOs.CategoryDtos;

namespace NFIT.Application.Validations.CategoryCreateValidations;

public class CategoryCreateValidator:AbstractValidator<CategoryCreateDto>
{
    public CategoryCreateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .Length(2, 100).WithMessage("Name must be 2-100 characters.");
        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description != null);
    }
}
