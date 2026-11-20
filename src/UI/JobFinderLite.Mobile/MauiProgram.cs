using JobFinderLite.Application.Interfaces;
using JobFinderLite.Application.ViewModels;
using JobFinderLite.Infrastructure.Configuration;
using JobFinderLite.Infrastructure.Services;
using JobFinderLite.Mobile.Services;
using JobFinderLite.Mobile.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobFinderLite.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // -------------------------------------------------------------
        // Dependency Injection Setup with deliberate lifetime choices:
        // -------------------------------------------------------------

        // 1. Configuration (Singleton)
        // Resolves API Base URL dynamically per platform:
        // Android Emulator uses 10.0.2.2:5000, iOS/Mac uses localhost:5000
        var apiOptions = new ApiOptions
        {
            BaseUrl = Environment.GetEnvironmentVariable("JOBS_API_URL") 
                      ?? ApiOptions.GetDefaultBaseUrlForPlatform(),
            TimeoutSeconds = 8,
            MaxRetries = 3
        };
        builder.Services.AddSingleton(Options.Create(apiOptions));

        // 2. HTTP Infrastructure & Resilience (Singleton)
        builder.Services.AddSingleton<HttpClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ApiOptions>>().Value;
            return new HttpClient
            {
                BaseAddress = new Uri(options.BaseUrl),
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
            };
        });

        // 3. Application & Infrastructure Services (Singletons for state and cached lifecycle)
        builder.Services.AddSingleton<IDispatcherService, MauiDispatcherService>();
        builder.Services.AddSingleton<IJobsApiClient, JobsApiClient>();
        builder.Services.AddSingleton<IJobCacheService, LocalJobCacheService>();
        builder.Services.AddSingleton<IApplicationStateService, InMemoryApplicationStateService>();
        builder.Services.AddSingleton<INavigationService, MauiShellNavigationService>();
        builder.Services.AddSingleton<IDialogService, MauiDialogService>();

        // 4. ViewModels (Transient - created per navigation, clean lifecycle)
        builder.Services.AddTransient<JobListViewModel>();
        builder.Services.AddTransient<JobDetailViewModel>();

        // 5. Views (Transient - resolves matching ViewModel automatically)
        builder.Services.AddTransient<JobListPage>();
        builder.Services.AddTransient<JobDetailPage>();

        return builder.Build();
    }
}
