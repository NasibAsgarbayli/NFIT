using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.OrderDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Domain.Enums;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class OrderService:IOrderService
{
    private readonly NFITDbContext _context;
    private readonly IHttpContextAccessor _http;
    private readonly IEmailService _email;
    private readonly UserManager<AppUser> _userManager;

    public OrderService(
        NFITDbContext context,
        IHttpContextAccessor http,
        IEmailService email,
        UserManager<AppUser> userManager)
    {
        _context = context;
        _http = http;
        _email = email;
        _userManager = userManager;
    }

    private string? UserId =>
        _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? _http.HttpContext?.User?.FindFirst("sub")?.Value;

    private string? UserEmail =>
        _http.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

    // ----------------- CREATE: SUPPLEMENTS -----------------
    public async Task<BaseResponse<Guid>> CreateSupplementOrderAsync(OrderCreateSupplementDto dto)
    {
        if (string.IsNullOrWhiteSpace(UserId))
            return new BaseResponse<Guid>("Unauthorized", Guid.Empty, HttpStatusCode.Unauthorized);

        if (dto.Items is null || dto.Items.Count == 0)
            return new BaseResponse<Guid>("Cart is empty", Guid.Empty, HttpStatusCode.BadRequest);

        var ids = dto.Items.Select(i => i.SupplementId).ToHashSet();
        var supplements = await _context.Supplements
            .Where(s => ids.Contains(s.Id) && !s.IsDeleted && s.IsActive)
            .ToListAsync();

        if (supplements.Count != ids.Count)
            return new BaseResponse<Guid>("Some supplements not found or inactive", Guid.Empty, HttpStatusCode.BadRequest);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = UserId!,
            OrderDate = DateTime.UtcNow,
            PaymentMethod = dto.PaymentMethod,
            Note = dto.Note,
            DeliveryAddress = dto.DeliveryAddress,
            Status = SupplementOrderStatus.Pending,
            OrderSupplements = new List<OrderSupplement>()
        };

        decimal total = 0m;
        foreach (var item in dto.Items)
        {
            var sup = supplements.First(s => s.Id == item.SupplementId);
            var unit = sup.Price; // snapshot

            order.OrderSupplements.Add(new OrderSupplement
            {
                Id = Guid.NewGuid(),
                UserId = UserId!,
                SupplementId = sup.Id,
                SupplementPrice = unit,
                OrderId = order.Id,
                CreatedAt = DateTime.UtcNow
            });

            total += unit * item.Quantity;  // Quantity dto-dadır
            // Stok azaldılması siyasətinə görə: Confirm zamanı da edə bilərsən.
            // sup.StockQuantity -= item.Quantity;
        }

        order.TotalPrice = total;

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        // --- EMAIL: user + admin (PLACED) ---
        await SendPlacedEmailsAsync(order);

        return new BaseResponse<Guid>("Order created (pending)", order.Id, HttpStatusCode.Created);
    }

    // ----------------- CREATE: SUBSCRIPTION -----------------
    public async Task<BaseResponse<Guid>> CreateSubscriptionOrderAsync(OrderCreateSubscriptionDto dto)
    {
        if (string.IsNullOrWhiteSpace(UserId))
            return new BaseResponse<Guid>("Unauthorized", Guid.Empty, HttpStatusCode.Unauthorized);

        var plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == dto.SubscriptionPlanId && !p.IsDeleted);

        if (plan is null)
            return new BaseResponse<Guid>("Subscription plan not found", Guid.Empty, HttpStatusCode.NotFound);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = UserId!,
            OrderDate = DateTime.UtcNow,
            PaymentMethod = dto.PaymentMethod,
            Note = dto.Note,
            Status = SupplementOrderStatus.Pending,
            SubscriptionPlanId = plan.Id,
            TotalPrice = plan.Price
        };

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        // --- EMAIL: user + admin (PLACED) ---
        await SendPlacedEmailsAsync(order);

        return new BaseResponse<Guid>("Subscription order created (pending)", order.Id, HttpStatusCode.Created);
    }

    // ----------------- CONFIRM (payment OK) -----------------
    public async Task<BaseResponse<string>> ConfirmOrderAsync(Guid orderId)
    {
        if (string.IsNullOrWhiteSpace(UserId))
            return new BaseResponse<string>("Unauthorized", HttpStatusCode.Unauthorized);

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.Orders
                .Include(o => o.SubscriptionPlan)
                .Include(o => o.OrderSupplements).ThenInclude(os => os.Supplement)
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);

            if (order is null)
                return new BaseResponse<string>("Order not found", HttpStatusCode.NotFound);

            if (order.UserId != UserId)
                return new BaseResponse<string>("Forbidden", HttpStatusCode.Forbidden);

            if (order.Status == SupplementOrderStatus.Delivered)
                return new BaseResponse<string>("Order already completed", HttpStatusCode.Conflict);

            order.Status = SupplementOrderStatus.Delivered;
            order.UpdatedAt = DateTime.UtcNow;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            // Subscription varsa → Membership auto-create
            if (order.SubscriptionPlanId is not null)
            {
                var plan = order.SubscriptionPlan!;

                // varsa aktiv membership-i bitir
                var current = await _context.Memberships
                    .Where(m => m.UserId == UserId && m.IsActive && !m.IsDeleted && m.EndDate > DateTime.UtcNow)
                    .OrderByDescending(m => m.StartDate)
                    .FirstOrDefaultAsync();

                if (current is not null)
                {
                    current.IsActive = false;
                    current.EndDate = DateTime.UtcNow;
                    current.UpdatedAt = DateTime.UtcNow;
                }

                var start = DateTime.UtcNow;
                var end = plan.BillingCycle switch
                {
                    BillingCycle.Monthly => start.AddMonths(1),
                    BillingCycle.Yearly => start.AddYears(1),
                    _ => start.AddMonths(1)
                };

                var membership = new Membership
                {
                    Id = Guid.NewGuid(),
                    UserId = UserId!,
                    SubscriptionPlanId = plan.Id,
                    StartDate = start,
                    EndDate = end,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Memberships.AddAsync(membership);
                await _context.SaveChangesAsync();
            }

            await tx.CommitAsync();

            // --- EMAIL: user + admin (CONFIRMED) ---
            await SendConfirmedEmailsAsync(order);

            return new BaseResponse<string>("Order confirmed", HttpStatusCode.OK);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ----------------- GET: My Orders -----------------
    public async Task<BaseResponse<List<OrderGetDto>>> GetMyOrdersAsync()
    {
        if (string.IsNullOrWhiteSpace(UserId))
            return new BaseResponse<List<OrderGetDto>>("Unauthorized", null, HttpStatusCode.Unauthorized);

        var orders = await _context.Orders
            .Include(o => o.SubscriptionPlan)
            .Include(o => o.OrderSupplements).ThenInclude(os => os.Supplement)
            .Where(o => o.UserId == UserId && !o.IsDeleted)
            .OrderByDescending(o => o.OrderDate)
            .AsNoTracking()
            .ToListAsync();

        if (orders.Count == 0)
            return new BaseResponse<List<OrderGetDto>>("No orders found", null, HttpStatusCode.NotFound);

        var list = orders.Select(MapOrderToDto).ToList();
        return new BaseResponse<List<OrderGetDto>>("Orders retrieved", list, HttpStatusCode.OK);
    }

    // ----------------- GET: Sales (Admin/Moderator) -----------------
    public async Task<BaseResponse<List<OrderGetDto>>> GetSalesAsync(SalesFilterDto? filter = null)
    {
        var q = _context.Orders
            .Include(o => o.SubscriptionPlan)
            .Include(o => o.OrderSupplements).ThenInclude(os => os.Supplement)
            .Where(o => !o.IsDeleted &&
                        (o.Status == SupplementOrderStatus.Delivered || o.Status == SupplementOrderStatus.Pending));

        if (filter?.From is not null) q = q.Where(o => o.OrderDate >= filter.From.Value);
        if (filter?.To is not null) q = q.Where(o => o.OrderDate <= filter.To.Value);

        var orders = await q.OrderByDescending(o => o.OrderDate).AsNoTracking().ToListAsync();
        if (orders.Count == 0)
            return new BaseResponse<List<OrderGetDto>>("No sales in range", null, HttpStatusCode.NotFound);

        var list = orders.Select(MapOrderToDto).ToList();
        return new BaseResponse<List<OrderGetDto>>("Sales retrieved", list, HttpStatusCode.OK);
    }

    // ----------------- UPDATE: Status -----------------
    public async Task<BaseResponse<string>> UpdateStatusAsync(Guid orderId, OrderStatusUpdateDto dto)
    {
        var o = await _context.Orders.FirstOrDefaultAsync(x => x.Id == orderId && !x.IsDeleted);
        if (o is null)
            return new BaseResponse<string>("Order not found", HttpStatusCode.NotFound);

        o.Status = dto.Status;
        o.UpdatedAt = DateTime.UtcNow;

        _context.Orders.Update(o);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("Order status updated", HttpStatusCode.OK);
    }

    // ----------------- DELETE (soft) -----------------
    public async Task<BaseResponse<string>> DeleteAsync(Guid orderId)
    {
        var o = await _context.Orders.FirstOrDefaultAsync(x => x.Id == orderId && !x.IsDeleted);
        if (o is null)
            return new BaseResponse<string>("Order not found", HttpStatusCode.NotFound);

        o.IsDeleted = true;
        o.UpdatedAt = DateTime.UtcNow;

        _context.Orders.Update(o);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("Order deleted", HttpStatusCode.OK);
    }

    // ===================== EMAIL HELPERS =====================
    private async Task<List<string>> GetAdminEmailsAsync()
    {
        var admins = await _userManager.GetUsersInRoleAsync("Admin"); // Səndə permission-policy varsa, uyğun rolu istifadə et
        return admins
            .Where(u => !string.IsNullOrWhiteSpace(u.Email))
            .Select(u => u.Email!.Trim())
            .Distinct()
            .ToList();
    }

    private async Task SendPlacedEmailsAsync(Order order)
    {
        var userSubject = order.SubscriptionPlanId is null
            ? $"Order #{order.Id} placed — total {order.TotalPrice:C}"
            : $"Subscription order #{order.Id} placed — {order.TotalPrice:C}";

        var adminSubject = order.SubscriptionPlanId is null
            ? $"[ADMIN] New supplement order #{order.Id} — {order.TotalPrice:C}"
            : $"[ADMIN] New subscription order #{order.Id} — {order.TotalPrice:C}";

        var userBody = BuildOrderPlacedEmailHtml(order, isAdmin: false);
        var adminBody = BuildOrderPlacedEmailHtml(order, isAdmin: true);

        if (!string.IsNullOrWhiteSpace(UserEmail))
            await _email.SendEmailAsync(new[] { UserEmail! }, userSubject, userBody);

        var adminEmails = await GetAdminEmailsAsync();
        if (adminEmails.Count > 0)
            await _email.SendEmailAsync(adminEmails, adminSubject, adminBody);
    }

    private async Task SendConfirmedEmailsAsync(Order order)
    {
        var userSubject = order.SubscriptionPlanId is null
            ? $"Order #{order.Id} confirmed — {order.TotalPrice:C}"
            : $"Subscription activated — order #{order.Id}";

        var adminSubject = order.SubscriptionPlanId is null
            ? $"[ADMIN] Order #{order.Id} confirmed — {order.TotalPrice:C}"
            : $"[ADMIN] Subscription confirmed — order #{order.Id}";

        var userBody = BuildOrderConfirmedEmailHtml(order, isAdmin: false);
        var adminBody = BuildOrderConfirmedEmailHtml(order, isAdmin: true);

        if (!string.IsNullOrWhiteSpace(UserEmail))
            await _email.SendEmailAsync(new[] { UserEmail! }, userSubject, userBody);

        var adminEmails = await GetAdminEmailsAsync();
        if (adminEmails.Count > 0)
            await _email.SendEmailAsync(adminEmails, adminSubject, adminBody);
    }

    private static string BuildOrderPlacedEmailHtml(Order order, bool isAdmin) => BuildEmailHtml(order,
        titleUser: "Sifarişiniz qeydə alındı",
        titleAdmin: "Yeni sifariş alındı",
        isAdmin: isAdmin);

    private static string BuildOrderConfirmedEmailHtml(Order order, bool isAdmin) => BuildEmailHtml(order,
        titleUser: "Sifarişiniz təsdiqləndi",
        titleAdmin: "Sifariş təsdiqləndi",
        isAdmin: isAdmin);

    private static string BuildEmailHtml(Order order, string titleUser, string titleAdmin, bool isAdmin)
    {
        var title = isAdmin ? titleAdmin : titleUser;

        var sb = new StringBuilder();
        sb.Append("""
        <html>
        <body style="font-family:Arial,Helvetica,sans-serif; color:#111;">
          <div style="max-width:640px;margin:auto;">
        """);

        sb.Append($"<h2 style='margin-bottom:6px;'>{title} — #{order.Id}</h2>");
        sb.Append($"<p style='margin-top:0;color:#666'>Tarix: {order.OrderDate:yyyy-MM-dd HH:mm}</p>");

        if (order.SubscriptionPlan is not null)
        {
            sb.Append($"""
            <h3 style="margin-top:18px;">Subscription</h3>
            <table cellpadding="6" cellspacing="0" style="border-collapse:collapse;width:100%;">
              <tr style="background:#f5f5f5">
                <th align="left">Plan</th>
                <th align="left">Type</th>
                <th align="left">Billing</th>
                <th align="right">Price</th>
              </tr>
              <tr>
                <td>{order.SubscriptionPlan.Name}</td>
                <td>{order.SubscriptionPlan.Type}</td>
                <td>{order.SubscriptionPlan.BillingCycle}</td>
                <td align="right">{order.TotalPrice:C}</td>
              </tr>
            </table>
            """);
        }

        if (order.OrderSupplements?.Any() == true)
        {
            sb.Append("""
            <h3 style="margin-top:18px;">Məhsullar</h3>
            <table cellpadding="6" cellspacing="0" style="border-collapse:collapse;width:100%;">
              <tr style="background:#f5f5f5">
                <th align="left">Məhsul</th>
                <th align="center">Ədəd</th>
                <th align="right">Qiymət</th>
                <th align="right">Cəm</th>
              </tr>
            """);

            foreach (var line in order.OrderSupplements)
            {
                var name = line.Supplement?.Name ?? "Supplement";
                var unit = line.SupplementPrice;
                var qty = 1; // Quantity yoxdursa 1
                sb.Append($"""
                  <tr>
                    <td>{name}</td>
                    <td align="center">{qty}</td>
                    <td align="right">{unit:C}</td>
                    <td align="right">{unit * qty:C}</td>
                  </tr>
                """);
            }

            sb.Append("</table>");
        }

        sb.Append($"""
          <h3 style="margin-top:18px;">Ümumi məlumat</h3>
          <p style="margin:6px 0;">Ödəniş metodu: <b>{order.PaymentMethod}</b></p>
        """);

        if (!string.IsNullOrWhiteSpace(order.DeliveryAddress))
            sb.Append($"<p style='margin:6px 0;'>Çatdırılma ünvanı: <b>{order.DeliveryAddress}</b></p>");

        if (!string.IsNullOrWhiteSpace(order.Note))
            sb.Append($"<p style='margin:6px 0;'>Qeyd: <i>{System.Net.WebUtility.HtmlEncode(order.Note)}</i></p>");

        sb.Append($"""
          <p style="margin:10px 0 0 0; font-size:16px;">Ümumi məbləğ: <b>{order.TotalPrice:C}</b></p>
          <hr style="border:none;border-top:1px solid #eee;margin:20px 0" />
          <p style="color:#666;font-size:12px;">Bu e-poçt avtomatik göndərilib.</p>
          </div>
        </body>
        </html>
        """);

        return sb.ToString();
    }

    // ===================== MAP HELPERS =====================
    private static OrderGetDto MapOrderToDto(Order o) => new()
    {
        Id = o.Id,
        OrderDate = o.OrderDate,
        Status = o.Status,
        PaymentMethod = o.PaymentMethod,
        TotalPrice = o.TotalPrice,
        Note = o.Note,
        DeliveryAddress = o.DeliveryAddress,
        SubscriptionPlanId = o.SubscriptionPlanId,
        SubscriptionPlanName = o.SubscriptionPlan?.Name,
        Lines = o.OrderSupplements?.Select(os => new OrderGetDto.OrderSupplementLine
        {
            SupplementId = os.SupplementId,
            Name = os.Supplement?.Name ?? "",
            Quantity = 1, // Quantity yoxdursa 1
            UnitPrice = os.SupplementPrice
        }).ToList() ?? new()
    };
}
