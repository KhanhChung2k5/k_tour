using HeriStepAI.Mobile.ViewModels;
using HeriStepAI.Mobile.Helpers;

namespace HeriStepAI.Mobile.Views;

public partial class POIListPage : ContentPage
{
    public POIListPage() : this(GetViewModel()) { }

    public POIListPage(POIListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // Apply responsive padding
        SearchBar.Padding = ResponsiveHelper.HeaderPadding();
    }

    static POIListViewModel GetViewModel() =>
        App.Services?.GetService<POIListViewModel>()
        ?? throw new InvalidOperationException("POIListViewModel not found. Check DI.");
}
