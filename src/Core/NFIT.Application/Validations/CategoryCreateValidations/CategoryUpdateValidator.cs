using FluentValidation;
using NFIT.Application.DTOs.CategoryDtos;

namespace NFIT.Application.Validations.CategoryCreateValidations;

public class CategoryUpdateValidator:AbstractValidator<CategoryUpdateDto>
{
    public CategoryUpdateValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().Length(2, 100);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
    }


}
