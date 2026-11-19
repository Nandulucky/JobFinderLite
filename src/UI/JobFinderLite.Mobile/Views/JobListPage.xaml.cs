using JobFinderLite.Application.ViewModels;

namespace JobFinderLite.Mobile.Views;

public partial class JobListPage : ContentPage
{
    private readonly JobListViewModel _viewModel;

    public JobListPage(JobListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
