using Microsoft.AspNetCore.Http;

namespace NFIT.Application.DTOs.TrainerDtos;

public class TrainerImageUploadPhotoDto
{
    public IFormFile File { get; set; }
}
