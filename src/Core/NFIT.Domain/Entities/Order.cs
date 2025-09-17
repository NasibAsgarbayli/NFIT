using NFIT.Domain.Enums;

namespace NFIT.Domain.Entities;

public class Order:BaseEntity
{
    public decimal? TotalPrice { get; set; } // Sifarişin ümumi məbləği
    public string? Note { get; set; }        // Buyer və ya admin üçün qeyd
    public string? DeliveryAddress { get; set; } // Çatdırılma ünvanı
    public PaymentMethod PaymentMethod { get; set; } 
    public DateTime OrderDate { get; set; }

    public string UserId { get; set; } = null!;
    public AppUser User { get; set; } = null!;

    public SupplementOrderStatus Status { get; set; } = SupplementOrderStatus.Pending;

    public ICollection<OrderSupplement> OrderSupplements { get; set; }
   
}
