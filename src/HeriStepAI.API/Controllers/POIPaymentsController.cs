using HeriStepAI.API.Data;
using HeriStepAI.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HeriStepAI.API.Controllers;

[ApiController]
[Route("api/poi-payments")]
public class POIPaymentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public POIPaymentsController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>ShopOwner tạo bản ghi thanh toán sau khi POI được tạo.</summary>
    [HttpPost("report")]
    [Authorize(Roles = "ShopOwner")]
    public async Task<IActionResult> Report([FromBody] ReportPOIPaymentDto? body)
    {
        if (body is null)
            return BadRequest(new { Message = "Body required" });

        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(ownerIdClaim, out var ownerId))
            return Unauthorized();

        var poi = await _db.POIs.FirstOrDefaultAsync(p => p.Id == body.POIId && p.OwnerId == ownerId);
        if (poi == null)
            return NotFound(new { Message = "POI không tồn tại hoặc không thuộc quyền sở hữu của bạn." });

        // Nếu đã có bản ghi Pending/Verified → không tạo thêm
        var existing = await _db.POIPayments
            .FirstOrDefaultAsync(p => p.POIId == body.POIId &&
                (p.Status == PaymentReconciliationStatus.Pending || p.Status == PaymentReconciliationStatus.Verified));
        if (existing != null)
            return Ok(new { id = existing.Id, duplicate = true, transferRef = existing.TransferRef, message = "Đã có bản ghi thanh toán." });

        var amount = POIPricing.GetPrice(poi.Priority);
        var transferRef = $"POIPAY-{poi.Id}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        var payment = new POIPayment
        {
            POIId = poi.Id,
            OwnerId = ownerId,
            Priority = poi.Priority,
            AmountVnd = amount,
            TransferRef = transferRef,
            Status = PaymentReconciliationStatus.Pending,
            ReportedAtUtc = DateTime.UtcNow
        };

        _db.POIPayments.Add(payment);
        await _db.SaveChangesAsync();

        return Ok(new { id = payment.Id, duplicate = false, transferRef, amount, message = "Đã ghi nhận. Vui lòng chờ Admin đối soát." });
    }

    /// <summary>Lấy trạng thái thanh toán của một POI (ShopOwner xem của mình).</summary>
    [HttpGet("by-poi/{poiId:int}")]
    [Authorize(Roles = "ShopOwner,Admin")]
    public async Task<IActionResult> GetByPOI(int poiId)
    {
        var ownerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(ownerIdClaim, out var requesterId);
        var isAdmin = User.IsInRole("Admin");

        var payment = await _db.POIPayments
            .AsNoTracking()
            .Where(p => p.POIId == poiId && (isAdmin || p.OwnerId == requesterId))
            .OrderByDescending(p => p.Id)
            .Select(p => new
            {
                p.Id, p.POIId, p.Priority, p.AmountVnd, p.TransferRef,
                Status = p.Status.ToString(), p.ReportedAtUtc, p.VerifiedAtUtc, p.AdminNote
            })
            .FirstOrDefaultAsync();

        if (payment == null) return NotFound(new { Message = "Chưa có bản ghi thanh toán." });
        return Ok(payment);
    }

    /// <summary>Danh sách tất cả thanh toán POI (Admin).</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] int take = 200)
    {
        take = Math.Clamp(take, 1, 500);
        var q = _db.POIPayments
            .AsNoTracking()
            .Include(p => p.POI)
            .Include(p => p.Owner)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentReconciliationStatus>(status, true, out var st))
            q = q.Where(p => p.Status == st);

        var rows = await q
            .OrderByDescending(p => p.ReportedAtUtc)
            .Take(take)
            .Select(p => new
            {
                p.Id,
                p.POIId,
                POIName = p.POI != null ? p.POI.Name : "",
                OwnerName = p.Owner != null ? (p.Owner.FullName ?? p.Owner.Username) : "",
                p.Priority,
                PriorityLabel = p.Priority == 1 ? "Thấp" : p.Priority == 2 ? "Trung bình" : "Cao",
                p.AmountVnd,
                p.TransferRef,
                Status = p.Status.ToString(),
                p.ReportedAtUtc,
                p.VerifiedAtUtc,
                p.VerifiedByUserId,
                p.AdminNote
            })
            .ToListAsync();

        return Ok(rows);
    }

    /// <summary>Tổng hợp cho dashboard Admin.</summary>
    [HttpGet("summary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Summary()
    {
        var pending = await _db.POIPayments.CountAsync(p => p.Status == PaymentReconciliationStatus.Pending);
        var verified = await _db.POIPayments.CountAsync(p => p.Status == PaymentReconciliationStatus.Verified);
        var rejected = await _db.POIPayments.CountAsync(p => p.Status == PaymentReconciliationStatus.Rejected);
        var totalVerified = await _db.POIPayments
            .Where(p => p.Status == PaymentReconciliationStatus.Verified)
            .SumAsync(p => p.AmountVnd);

        return Ok(new { pending, verified, rejected, totalAmountVndVerified = totalVerified });
    }

    public class VerifyDto { public string? Note { get; set; } }

    /// <summary>Admin xác nhận → POI.IsActive = true.</summary>
    [HttpPost("{id:int}/verify")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Verify(int id, [FromBody] VerifyDto? dto)
    {
        var row = await _db.POIPayments.AsTracking().Include(p => p.POI).FirstOrDefaultAsync(p => p.Id == id);
        if (row == null) return NotFound(new { Message = "Không tìm thấy bản ghi." });

        var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? adminId = int.TryParse(adminIdClaim, out var uid) ? uid : null;

        row.Status = PaymentReconciliationStatus.Verified;
        row.VerifiedAtUtc = DateTime.UtcNow;
        row.VerifiedByUserId = adminId;
        if (!string.IsNullOrWhiteSpace(dto?.Note))
            row.AdminNote = dto.Note.Trim()[..Math.Min(dto.Note.Length, 500)];

        // Kích hoạt POI
        if (row.POI != null)
        {
            row.POI.IsActive = true;
            row.POI.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new { Message = "Đã xác nhận. POI đã được kích hoạt." });
    }

    /// <summary>Admin từ chối → POI vẫn inactive.</summary>
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(int id, [FromBody] VerifyDto? dto)
    {
        var row = await _db.POIPayments.AsTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (row == null) return NotFound(new { Message = "Không tìm thấy bản ghi." });

        var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? adminId = int.TryParse(adminIdClaim, out var uid) ? uid : null;

        row.Status = PaymentReconciliationStatus.Rejected;
        row.VerifiedAtUtc = DateTime.UtcNow;
        row.VerifiedByUserId = adminId;
        if (!string.IsNullOrWhiteSpace(dto?.Note))
            row.AdminNote = dto.Note.Trim()[..Math.Min(dto.Note.Length, 500)];

        await _db.SaveChangesAsync();
        return Ok(new { Message = "Đã từ chối. POI không được kích hoạt." });
    }

    public class ReportPOIPaymentDto
    {
        public int POIId { get; set; }
    }
}
