using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Repositories;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.OrderDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Domain.Enums;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class OrderService:IOrderService
{
    private readonly IOrderRepository _orders;
    private readonly ISupplementRepository _supplements;
    private readonly ISubscriptionPlanRepository _plans;
    private readonly IGymRepository _gyms; // sales/filter üçün lazım ola bilər
    private readonly IHttpContextAccessor _http;
    private readonly IEmailService _email;
    private readonly UserManager<AppUser> _userManager;

    public OrderService(
        IOrderRepository orders,
        ISupplementRepository supplements,
        ISubscriptionPlanRepository plans,
        IGymRepository gyms,
        IHttpContextAccessor http,
        IEmailService email,
        UserManager<AppUser> userManager)
    {
        _orders = orders;
        _supplements = supplements;
        _plans = plans;
        _gyms = gyms;
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
            return new("Unauthorized", Guid.Empty, HttpStatusCode.Unauthorized);

        if (dto?.Items is null || dto.Items.Count == 0)
            return new("Cart is empty", Guid.Empty, HttpStatusCode.BadRequest);

        if (!Enum.IsDefined(typeof(PaymentMethod), dto.PaymentMethod))
            return new("Invalid payment method", Guid.Empty, HttpStatusCode.BadRequest);

        var merged = dto.Items
            .GroupBy(i => i.SupplementId)
            .Select(g => new { SupplementId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToList();

        if (merged.Any(x => x.Quantity <= 0))
            return new("Quantity must be greater than 0", Guid.Empty, HttpStatusCode.BadRequest);

        var ids = merged.Select(i => i.SupplementId).ToHashSet();

        var supplements = await _supplements
            .GetByFiltered(s => ids.Contains(s.Id) && !s.IsDeleted && s.IsActive, IsTracking: false)
            .ToListAsync();

        if (supplements.Count != ids.Count)
            return new("Some supplements not found or inactive", Guid.Empty, HttpStatusCode.BadRequest);

        foreach (var item in merged)
        {
            var sup = supplements.First(s => s.Id == item.SupplementId);
            if (sup.StockQuantity < item.Quantity)
                return new($"Insufficient stock for {sup.Name}", Guid.Empty, HttpStatusCode.Conflict);
        }

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
        foreach (var item in merged)
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
                Quantity = item.Quantity,
                CreatedAt = DateTime.UtcNow,
            });

            total += unit * item.Quantity;
        }

        order.TotalPrice = total;

        await _orders.AddAsync(order);
        await _orders.SaveChangeAsync();

        var placedOrder = await _orders
            .GetByFiltered(o => o.Id == order.Id,
                include: new[] { (System.Linq.Expressions.Expression<Func<Order, object>>)(o => o.SubscriptionPlan),
                                 o => o.OrderSupplements!})
            .FirstAsync();

        await SendPlacedEmailsAsync(placedOrder);
        return new("Order created (pending)", order.Id, HttpStatusCode.Created);
    }

    // ----------------- CREATE: SUBSCRIPTION -----------------
    public async Task<BaseResponse<Guid>> CreateSubscriptionOrderAsync(OrderCreateSubscriptionDto dto)
    {
        if (string.IsNullOrWhiteSpace(UserId))
            return new("Unauthorized", Guid.Empty, HttpStatusCode.Unauthorized);

        if (!Enum.IsDefined(typeof(PaymentMethod), dto.PaymentMethod))
            return new("Invalid payment method", Guid.Empty, HttpStatusCode.BadRequest);

        var plan = await _plans
            .GetByFiltered(p => p.Id == dto.SubscriptionPlanId && !p.IsDeleted, IsTracking: false)
            .FirstOrDefaultAsync();

        if (plan is null)
            return new("Subscription plan not found", Guid.Empty, HttpStatusCode.NotFound);

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

        await _orders.AddAsync(order);
        await _orders.SaveChangeAsync();

        var placedOrder = await _orders
            .GetByFiltered(o => o.Id == order.Id,
                include: new[] { (System.Linq.Expressions.Expression<Func<Order, object>>)(o => o.SubscriptionPlan),
                                 o => o.OrderSupplements!})
            .FirstAsync();

        await SendPlacedEmailsAsync(placedOrder);
        return new("Subscription order created (pending)", order.Id, HttpStatusCode.Created);
    }

    // ----------------- CONFIRM (payment OK) -----------------
    public async Task<BaseResponse<string>> ConfirmOrderAsync(Guid orderId)
    {
        if (string.IsNullOrWhiteSpace(UserId))
            return new("Unauthorized", HttpStatusCode.Unauthorized);

        var order = await _orders
            .GetByFiltered(o => o.Id == orderId && !o.IsDeleted,
                           include: new[] { (System.Linq.Expressions.Expression<Func<Order, object>>)(o => o.SubscriptionPlan),
                                            o => o.OrderSupplements!.Select(os => os.Supplement!) })
            .FirstOrDefaultAsync();

        if (order is null)
            return new("Order not found", HttpStatusCode.NotFound);

        if (order.UserId != UserId)
            return new("Forbidden", HttpStatusCode.Forbidden);

        if (order.Status == SupplementOrderStatus.Delivered)
            return new("Order already completed", HttpStatusCode.Conflict);

        // Supplement-li sifarişlər üçün stok azalt
        if (order.OrderSupplements?.Any() == true)
        {
            // bütün dəyişiklikləri yadda saxlamamışdan əvvəl RAM-da edirik
            var now = DateTime.UtcNow;
            foreach (var line in order.OrderSupplements)
            {
                var sup = await _supplements
                    .GetByFiltered(s => s.Id == line.SupplementId && !s.IsDeleted && s.IsActive)
                    .FirstOrDefaultAsync();

                if (sup is null)
                    return new("Supplement not found during confirmation", HttpStatusCode.Conflict);

                if (sup.StockQuantity < line.Quantity)
                    return new($"Insufficient stock for {sup.Name}", HttpStatusCode.Conflict);

                sup.StockQuantity -= line.Quantity;
                sup.UpdatedAt = now;
                _supplements.Update(sup);
            }

            // statusu dəyiş
            order.Status = SupplementOrderStatus.Delivered;
            order.UpdatedAt = DateTime.UtcNow;
            _orders.Update(order);

            // hamısını BİRDƏFƏ yaz
            await _orders.SaveChangeAsync();
        }
        else
        {
            order.Status = SupplementOrderStatus.Delivered;
            order.UpdatedAt = DateTime.UtcNow;
            _orders.Update(order);
            await _orders.SaveChangeAsync();
        }

        await SendConfirmedEmailsAsync(order);
        // Qeyd: membership sonradan "memberships/from-order" ilə yaradılır.
        return new("Order confirmed (Delivered)", HttpStatusCode.OK);
    }

    // ----------------- GET: My Orders -----------------
    public async Task<BaseResponse<List<OrderGetDto>>> GetMyOrdersAsync()
    {
        if (string.IsNullOrWhiteSpace(UserId))
            return new("Unauthorized", null, HttpStatusCode.Unauthorized);

        var orders = await _orders
            .GetByFiltered(o => o.UserId == UserId && !o.IsDeleted,
                include: new[] { (System.Linq.Expressions.Expression<Func<Order, object>>)(o => o.SubscriptionPlan),
                                 o => o.OrderSupplements! },
                IsTracking: false)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        if (orders.Count == 0)
            return new("No orders found", null, HttpStatusCode.NotFound);

        var list = orders.Select(MapOrderToDto).ToList();
        return new("Orders retrieved", list, HttpStatusCode.OK);
    }

    // ----------------- GET: Sales (Admin/Moderator) -----------------
    public async Task<BaseResponse<List<OrderGetDto>>> GetSalesAsync(SalesFilterDto? filter = null)
    {
        if (filter?.From is not null && filter?.To is not null && filter.From > filter.To)
            return new("Invalid date range", null, HttpStatusCode.BadRequest);

        var q = _orders
            .GetByFiltered(o => !o.IsDeleted &&
                    (o.Status == SupplementOrderStatus.Delivered || o.Status == SupplementOrderStatus.Pending),
                include: new[] { (System.Linq.Expressions.Expression<Func<Order, object>>)(o => o.SubscriptionPlan),
                                 o => o.OrderSupplements ! },
                IsTracking: false);

        if (filter?.From is not null) q = q.Where(o => o.OrderDate >= filter.From.Value);
        if (filter?.To is not null) q = q.Where(o => o.OrderDate <= filter.To.Value);

        var orders = await q.OrderByDescending(o => o.OrderDate).ToListAsync();
        if (orders.Count == 0)
            return new("No sales in range", null, HttpStatusCode.NotFound);

        var list = orders.Select(MapOrderToDto).ToList();
        return new("Sales retrieved", list, HttpStatusCode.OK);
    }

    // ----------------- UPDATE: Status -----------------
    public async Task<BaseResponse<string>> UpdateStatusAsync(Guid orderId, OrderStatusUpdateDto dto)
    {
        if (!Enum.IsDefined(typeof(SupplementOrderStatus), dto.Status))
            return new("Invalid status", HttpStatusCode.BadRequest);

        var o = await _orders.GetByFiltered(x => x.Id == orderId && !x.IsDeleted).FirstOrDefaultAsync();
        if (o is null) return new("Order not found", HttpStatusCode.NotFound);

        if (o.Status == SupplementOrderStatus.Delivered)
            return new("Order already completed", HttpStatusCode.Conflict);

        if (o.Status != SupplementOrderStatus.Pending && dto.Status == SupplementOrderStatus.Pending)
            return new("Cannot revert to pending", HttpStatusCode.BadRequest);

        o.Status = dto.Status;
        o.UpdatedAt = DateTime.UtcNow;

        _orders.Update(o);
        await _orders.SaveChangeAsync();

        return new("Order status updated", HttpStatusCode.OK);
    }

    // ----------------- DELETE (soft) -----------------
    public async Task<BaseResponse<string>> DeleteAsync(Guid orderId)
    {
        var o = await _orders.GetByFiltered(x => x.Id == orderId && !x.IsDeleted).FirstOrDefaultAsync();
        if (o is null) return new("Order not found", HttpStatusCode.NotFound);

        o.IsDeleted = true;
        o.UpdatedAt = DateTime.UtcNow;

        _orders.Update(o);
        await _orders.SaveChangeAsync();

        return new("Order deleted", HttpStatusCode.OK);
    }

    // ===================== EMAIL HELPERS =====================
    private async Task<List<string>> GetAdminEmailsAsync()
    {
        var admins = await _userManager.GetUsersInRoleAsync("Admin");
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
            : $"Subscription confirmed — order #{order.Id}";

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

    private static string BuildOrderPlacedEmailHtml(Order order, bool isAdmin) =>
        BuildEmailHtml(order, "Sifarişiniz qeydə alındı", "Yeni sifariş alındı", isAdmin);

    private static string BuildOrderConfirmedEmailHtml(Order order, bool isAdmin) =>
        BuildEmailHtml(order, "Sifarişiniz təsdiqləndi", "Sifariş təsdiqləndi", isAdmin);

    // ⭐ Email HTML
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
                var qty = line.Quantity;
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

    // ===================== MAP =====================
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
            Quantity = os.Quantity,
            UnitPrice = os.SupplementPrice
        }).ToList() ?? new()
    };
}