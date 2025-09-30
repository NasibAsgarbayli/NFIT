using NFIT.Domain.Enums;

namespace NFIT.Domain.Entities;

public class OrderSupplement:BaseEntity
{
    public string UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public Guid SupplementId { get; set; }
    public Supplement Supplement { get; set; } = null!;
    public decimal SupplementPrice { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int Quantity { get; set; }

}
