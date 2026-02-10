using HeriStepAI.API.Models;

namespace HeriStepAI.Web.ViewModels;

public class ShopOwnerEditViewModel
{
    public POI POI { get; set; } = new();
    public List<POIContentEditModel> Contents { get; set; } = new();
}

public class POIContentEditModel
{
    public int Id { get; set; }
    public string Language { get; set; } = "vi";
    public string? TextContent { get; set; }
    public string? AudioUrl { get; set; }
    public int ContentType { get; set; } = 1; // 1=TTS, 2=AudioFile
}
