using Microsoft.AspNetCore.Http;

namespace NFIT.Application.DTOs.FileUploadDto;

public class FileUploadDto
{
    public IFormFile File { get; set; } = null!;

}
