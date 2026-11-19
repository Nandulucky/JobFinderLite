using JobFinderLite.Application.ViewModels;

namespace JobFinderLite.Mobile.Views;

[QueryProperty(nameof(JobId), "jobId")]
public partial class JobDetailPage : ContentPage
{
    private readonly JobDetailViewModel _viewModel;

    public string JobId
    {
        get => _viewModel.JobId;
        set => _viewModel.JobId = Uri.UnescapeDataString(value ?? string.Empty);
    }

    public JobDetailPage(JobDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
}
