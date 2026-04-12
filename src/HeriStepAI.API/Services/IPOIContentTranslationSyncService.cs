namespace HeriStepAI.API.Services;

/// <summary>
/// Dịch nội dung tiếng Việt sang các ngôn ngữ hỗ trợ và cập nhật/ghi đè bản ghi POIContent tương ứng.
/// </summary>
public interface IPOIContentTranslationSyncService
{
    /// <summary>
    /// Lấy bản thuyết minh tiếng Việt hiện có của POI, dịch sang en/ko/zh/ja/th/fr và cập nhật DB (ghi đè nếu đã có).
    /// </summary>
    Task SyncFromVietnameseAsync(int poiId, CancellationToken cancellationToken = default);
}
