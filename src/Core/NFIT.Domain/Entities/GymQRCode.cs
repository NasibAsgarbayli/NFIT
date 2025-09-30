namespace NFIT.Domain.Entities;

public class GymQRCode:BaseEntity
{
    public Guid GymId { get; set; }
    public Gym Gym { get; set; } = null!;

    public string QRCodeData { get; set; } = null!; // Unique QR data
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }  // <-- YENİ
    public bool IsOneTime { get; set; }       // <-- YENİ
    public DateTime? UsedAt { get; set; }     // <-- YENİ
}
