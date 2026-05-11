using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

/// <summary>Dịch vụ tour selection</summary>
public interface ITourSelectionService
{
    /// <summary>Tour được chọn</summary>
    Tour? SelectedTour { get; set; }
}
