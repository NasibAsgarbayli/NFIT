using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using NFIT.Application.Abstracts.Services;

namespace NFIT.Infrastructure.Services;

public class FileService:IFileService
{
    private readonly IWebHostEnvironment _env;
    public FileService(IWebHostEnvironment env)
    {
        _env = env;
    }
    public async Task<string> UploadAsync(IFormFile file)
    {
        var root = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(root))
            root = Path.Combine(AppContext.BaseDirectory, "wwwroot");

        var uploadsFolder = Path.Combine(root, "uploads"); 
        Directory.CreateDirectory(uploadsFolder);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/{fileName}";
    }
}
