using FluentValidation;
using NFIT.Application.DTOs.AuthenticationDtos;

namespace NFIT.Application.Validations.AuthenticationValidations;

public class UserRegisterDtoValidator:AbstractValidator<UserRegisterDto>
{
    public UserRegisterDtoValidator()
    {
        RuleFor(x => x.FullName)
           .NotEmpty().WithMessage("Fullname cannot be empty.")
           .MinimumLength(3).WithMessage("Fullname must be at least 3 characters long.")
           .MaximumLength(100).WithMessage("Fullname cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email cannot be empty.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password cannot be empty.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}
