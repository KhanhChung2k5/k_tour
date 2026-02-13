using HeriStepAI.Mobile.Helpers;
using HeriStepAI.Mobile.ViewModels;

namespace HeriStepAI.Mobile.Views;

public partial class TourDetailPage : ContentPage
{
    public TourDetailPage(TourDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // Apply responsive sizing
        HeaderGrid.HeightRequest = ResponsiveHelper.Height(170);
        HeaderContent.Padding = new Thickness(
            ResponsiveHelper.Spacing(16),
            ResponsiveHelper.Spacing(44),
            ResponsiveHelper.Spacing(16),
            ResponsiveHelper.Spacing(14)
        );
        ContentStack.Padding = new Thickness(
            ResponsiveHelper.Spacing(14),
            ResponsiveHelper.Spacing(12),
            ResponsiveHelper.Spacing(14),
            ResponsiveHelper.Spacing(20)  // Reduced from 70 to 20
        );
    }
}
