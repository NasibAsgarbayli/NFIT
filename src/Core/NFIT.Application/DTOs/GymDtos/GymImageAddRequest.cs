using Microsoft.AspNetCore.Http;

namespace NFIT.Application.DTOs.GymDtos;

public class GymImageAddRequest
{
    public IFormFile Image { get; set; } 
}
