using HeriStepAI.API.Data;
using HeriStepAI.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HeriStepAI.API.Services;

public class POIContentTranslationSyncService : IPOIContentTranslationSyncService
{
    private readonly ApplicationDbContext _context;
    private readonly ITranslationService _translation;

    private static readonly string[] TargetLangs = { "en", "ko", "zh", "ja", "th", "fr" };

    public POIContentTranslationSyncService(ApplicationDbContext context, ITranslationService translation)
    {
        _context = context;
        _translation = translation;
    }

    public async Task SyncFromVietnameseAsync(int poiId, CancellationToken cancellationToken = default)
    {
        var tracked = await _context.POIs
            .AsSplitQuery()
            .Include(p => p.Contents)
            .FirstOrDefaultAsync(p => p.Id == poiId, cancellationToken);

        if (tracked?.Contents == null || tracked.Contents.Count == 0)
            return;

        var vi = tracked.Contents.FirstOrDefault(c => c.Language == "vi" && !string.IsNullOrWhiteSpace(c.TextContent));
        if (vi?.TextContent == null)
        {
            Console.WriteLine($"[POIContentTranslationSync] POI {poiId}: không có nội dung tiếng Việt — bỏ qua.");
            return;
        }

        var source = vi.TextContent.Trim();
        if (string.IsNullOrEmpty(source))
            return;

        Console.WriteLine($"[POIContentTranslationSync] POI {poiId}: dịch từ tiếng Việt ({source.Length} ký tự)...");
        var translations = await _translation.TranslateToAllLanguagesAsync(source);

        foreach (var lang in TargetLangs)
        {
            if (!translations.TryGetValue(lang, out var text) || string.IsNullOrWhiteSpace(text))
            {
                Console.WriteLine($"[POIContentTranslationSync] ⚠ Không dịch được sang {lang}");
                continue;
            }

            var rows = tracked.Contents.Where(c => c.Language == lang).ToList();
            if (rows.Count == 0)
            {
                _context.POIContents.Add(new POIContent
                {
                    POId = poiId,
                    Language = lang,
                    TextContent = text,
                    ContentType = ContentType.TTS,
                    CreatedAt = DateTime.UtcNow
                });
                Console.WriteLine($"[POIContentTranslationSync] ✓ Thêm {lang}");
            }
            else
            {
                foreach (var row in rows)
                {
                    row.TextContent = text;
                }
                Console.WriteLine($"[POIContentTranslationSync] ✓ Cập nhật {lang} ({rows.Count} bản ghi)");
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
