using FluentValidation;
using NFIT.Application.DTOs.FileUploadDto;

namespace NFIT.Application.Validations.FileUploadDtoValidations;

public class FileUploadDtoValidator:AbstractValidator<FileUploadDto>
{
    public FileUploadDtoValidator()
    {
        RuleFor(f => f.File)
            .NotEmpty()
            .WithMessage("File can not be empty")
            .Must(file => file.Length > 0)
            .WithMessage("Fayl bos ola bilmez")
            .Must(file => file.Length <= 5 * 1024 * 1024)
            .WithMessage("Faylin olcusu 5mb-dan cox ola bilmez");
    }
}
