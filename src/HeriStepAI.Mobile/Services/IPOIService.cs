using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

/// <summary>Dịch vụ POI</summary>
public interface IPOIService
{
    /// <summary>Lấy danh sách địa điểm</summary>
    Task<List<POI>> GetAllPOIsAsync();
    /// <summary>Lấy địa điểm theo ID</summary>
    Task<POI?> GetPOIByIdAsync(int id);
    /// <summary>Đồng bộ địa điểm từ server</summary>
    Task SyncPOIsFromServerAsync();
}
