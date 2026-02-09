using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

public interface ITourSelectionService
{
    Tour? SelectedTour { get; set; }
}
