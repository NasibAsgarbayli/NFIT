using Microsoft.AspNetCore.Http;

namespace NFIT.Application.Abstracts.Services;

public interface IFileService
{
    Task<string> UploadAsync(IFormFile file);
}
