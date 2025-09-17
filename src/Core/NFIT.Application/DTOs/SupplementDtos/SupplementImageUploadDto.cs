using Microsoft.AspNetCore.Http;

namespace NFIT.Application.DTOs.SupplementDtos;

public class SupplementImageUploadDto
{
    public IFormFile File { get; set; }
}
