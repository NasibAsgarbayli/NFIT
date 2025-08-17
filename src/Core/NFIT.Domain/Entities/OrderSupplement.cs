using NFIT.Domain.Enums;

namespace NFIT.Domain.Entities;

public class OrderSupplement
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public Guid SupplementId { get; set; }
    public Supplement Supplement { get; set; } = null!;
    public decimal SupplementPrice { get; set; }
  
}
