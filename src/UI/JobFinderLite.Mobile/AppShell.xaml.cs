using JobFinderLite.Mobile.Views;

namespace JobFinderLite.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register route for Shell navigation
        Routing.RegisterRoute("jobdetail", typeof(JobDetailPage));
    }
}
