namespace NFIT.Application.DTOs.GymQrCodeDtos;

public class GymQrGetDto
{
    public Guid GymId { get; set; }
    public string QRCodeData { get; set; } = default!;
    public bool IsActive { get; set; }
}
