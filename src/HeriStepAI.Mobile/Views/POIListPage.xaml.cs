using HeriStepAI.Mobile.ViewModels;

namespace HeriStepAI.Mobile.Views;

public partial class POIListPage : ContentPage
{
    public POIListPage() : this(GetViewModel()) { }

    public POIListPage(POIListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    static POIListViewModel GetViewModel() =>
        App.Services?.GetService<POIListViewModel>()
        ?? throw new InvalidOperationException("POIListViewModel not found. Check DI.");
}
