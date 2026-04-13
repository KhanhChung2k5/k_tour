using HeriStepAI.API.Data;
using HeriStepAI.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeriStepAI.API.Controllers;

[ApiController]
[Route("api/subscription-payments")]
public class SubscriptionPaymentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public SubscriptionPaymentsController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>Tính ngày hết hạn gói kể từ thời điểm Admin xác nhận (PlanCode: D/W/M/Y).</summary>
    internal static DateTime ComputeExpiryUtcFromPlanCode(string? planCode, DateTime fromUtc)
    {
        var days = planCode?.Trim().ToUpperInvariant() switch
        {
            "D" => 1,
            "W" => 7,
            "M" => 30,
            "Y" => 365,
            _ => 30
        };
        return fromUtc.AddDays(days);
    }

    /// <summary>Báo từ app sau khi user xác nhận đã CK (để Admin đối soát). Cho phép anonymous.</summary>
    [HttpPost("report")]
    [AllowAnonymous]
    public async Task<IActionResult> Report([FromBody] ReportSubscriptionPaymentDto? body)
    {
        if (body is null)
            return BadRequest(new { Message = "Body required" });

        var device = (body.DeviceKey ?? "").Trim();
        var transferRef = (body.TransferRef ?? "").Trim();
        if (device.Length is < 4 or > 16)
            return BadRequest(new { Message = "DeviceKey không hợp lệ" });
        if (transferRef.Length is < 6 or > 64)
            return BadRequest(new { Message = "TransferRef không hợp lệ" });
        if (body.AmountVnd <= 0 || body.AmountVnd > 100_000_000)
            return BadRequest(new { Message = "AmountVnd không hợp lệ" });

        var planCode = (body.PlanCode ?? "").Trim();
        if (planCode.Length > 8)
            return BadRequest(new { Message = "PlanCode không hợp lệ" });

        // Tránh trùng: cùng máy + cùng mã CK trong 48h → trả bản ghi cũ
        var since = DateTime.UtcNow.AddHours(-48);
        var existing = await _db.MobileSubscriptionPayments
            .AsQueryable()
            .Where(p => p.DeviceKey == device && p.TransferRef == transferRef && p.ReportedAtUtc >= since)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            return Ok(new
            {
                id = existing.Id,
                duplicate = true,
                message = "Đã ghi nhận báo trước đó."
            });
        }

        var row = new MobileSubscriptionPayment
        {
            DeviceKey = device,
            TransferRef = transferRef,
            PlanCode = planCode,
            PlanLabel = string.IsNullOrWhiteSpace(body.PlanLabel) ? null : body.PlanLabel!.Trim()[..Math.Min(body.PlanLabel.Length, 64)],
            AmountVnd = body.AmountVnd,
            // Hết hạn gói chỉ gán khi Admin xác nhận đối soát (tránh user tự kích hoạt bằng báo giả)
            SubscriptionExpiresAtUtc = null,
            Platform = string.IsNullOrWhiteSpace(body.Platform) ? null : body.Platform.Trim()[..Math.Min(body.Platform.Length, 32)],
            Status = PaymentReconciliationStatus.Pending,
            ReportedAtUtc = DateTime.UtcNow
        };

        _db.MobileSubscriptionPayments.Add(row);
        await _db.SaveChangesAsync();

        return Ok(new { id = row.Id, duplicate = false, message = "Đã ghi nhận. Vui lòng chờ đối soát." });
    }

    /// <summary>App gọi để biết gói đã được Admin duyệt chưa (AllowAnonymous, theo DeviceKey).</summary>
    [HttpGet("entitlement")]
    [AllowAnonymous]
    public async Task<IActionResult> Entitlement([FromQuery] string? deviceKey)
    {
        var key = (deviceKey ?? "").Trim();
        if (key.Length is < 4 or > 16)
            return BadRequest(new { message = "DeviceKey không hợp lệ" });

        var now = DateTime.UtcNow;

        var active = await _db.MobileSubscriptionPayments
            .AsNoTracking()
            .Where(p =>
                p.DeviceKey == key
                && p.Status == PaymentReconciliationStatus.Verified
                && p.SubscriptionExpiresAtUtc != null
                && p.SubscriptionExpiresAtUtc > now)
            .OrderByDescending(p => p.SubscriptionExpiresAtUtc)
            .Select(p => new { p.PlanCode, ExpiresAtUtc = p.SubscriptionExpiresAtUtc })
            .FirstOrDefaultAsync();

        if (active != null)
        {
            return Ok(new
            {
                status = "active",
                planCode = active.PlanCode,
                expiresAtUtc = active.ExpiresAtUtc
            });
        }

        var hasPending = await _db.MobileSubscriptionPayments
            .AsNoTracking()
            .AnyAsync(p => p.DeviceKey == key && p.Status == PaymentReconciliationStatus.Pending);

        if (hasPending)
            return Ok(new { status = "pending" });

        return Ok(new { status = "none" });
    }

    public class ReportSubscriptionPaymentDto
    {
        public string? DeviceKey { get; set; }
        public string? TransferRef { get; set; }
        public string? PlanCode { get; set; }
        public string? PlanLabel { get; set; }
        public int AmountVnd { get; set; }
        public DateTime? SubscriptionExpiresAtUtc { get; set; }
        public string? Platform { get; set; }
    }

    /// <summary>Danh sách báo thanh toán (Admin).</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] int take = 200)
    {
        take = Math.Clamp(take, 1, 500);
        var q = _db.MobileSubscriptionPayments.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentReconciliationStatus>(status, true, out var st))
            q = q.Where(p => p.Status == st);

        var rows = await q
            .OrderByDescending(p => p.ReportedAtUtc)
            .Take(take)
            .Select(p => new
            {
                p.Id,
                p.DeviceKey,
                p.TransferRef,
                p.PlanCode,
                p.PlanLabel,
                p.AmountVnd,
                Status = p.Status.ToString(),
                p.ReportedAtUtc,
                p.SubscriptionExpiresAtUtc,
                p.Platform,
                p.VerifiedAtUtc,
                p.VerifiedByUserId,
                p.AdminNote
            })
            .ToListAsync();

        return Ok(rows);
    }

    /// <summary>Tổng hợp nhanh cho dashboard / báo cáo.</summary>
    [HttpGet("summary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Summary()
    {
        var pending = await _db.MobileSubscriptionPayments.CountAsync(p => p.Status == PaymentReconciliationStatus.Pending);
        var verified = await _db.MobileSubscriptionPayments.CountAsync(p => p.Status == PaymentReconciliationStatus.Verified);
        var rejected = await _db.MobileSubscriptionPayments.CountAsync(p => p.Status == PaymentReconciliationStatus.Rejected);

        var since7 = DateTime.UtcNow.AddDays(-7);
        var last7 = await _db.MobileSubscriptionPayments.CountAsync(p => p.ReportedAtUtc >= since7);

        var sumVerified = await _db.MobileSubscriptionPayments
            .Where(p => p.Status == PaymentReconciliationStatus.Verified)
            .SumAsync(p => (long)p.AmountVnd);

        return Ok(new
        {
            pending,
            verified,
            rejected,
            reportsLast7Days = last7,
            totalAmountVndVerified = sumVerified
        });
    }

    public class VerifyDto
    {
        public string? Note { get; set; }
    }

    [HttpPost("{id:int}/verify")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Verify(int id, [FromBody] VerifyDto? dto)
    {
        var row = await _db.MobileSubscriptionPayments.AsTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (row == null) return NotFound(new { Message = "Không tìm thấy bản ghi" });

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int? adminId = int.TryParse(userIdClaim, out var uid) ? uid : null;

        row.Status = PaymentReconciliationStatus.Verified;
        var verifiedAt = DateTime.UtcNow;
        row.VerifiedAtUtc = verifiedAt;
        row.VerifiedByUserId = adminId;
        row.AdminNote = string.IsNullOrWhiteSpace(dto?.Note) ? row.AdminNote : dto!.Note!.Trim()[..Math.Min(dto.Note.Length, 500)];
        row.SubscriptionExpiresAtUtc = ComputeExpiryUtcFromPlanCode(row.PlanCode, verifiedAt);

        await _db.SaveChangesAsync();
        return Ok(new { Message = "Đã xác nhận đối soát" });
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(int id, [FromBody] VerifyDto? dto)
    {
        var row = await _db.MobileSubscriptionPayments.AsTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (row == null) return NotFound(new { Message = "Không tìm thấy bản ghi" });

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int? adminId = int.TryParse(userIdClaim, out var uid) ? uid : null;

        row.Status = PaymentReconciliationStatus.Rejected;
        row.VerifiedAtUtc = DateTime.UtcNow;
        row.VerifiedByUserId = adminId;
        if (!string.IsNullOrWhiteSpace(dto?.Note))
            row.AdminNote = dto!.Note!.Trim()[..Math.Min(dto.Note.Length, 500)];

        await _db.SaveChangesAsync();
        return Ok(new { Message = "Đã đánh dấu từ chối / không khớp sao kê" });
    }
}
