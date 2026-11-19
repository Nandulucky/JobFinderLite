namespace JobFinderLite.Infrastructure.Configuration;

public class ApiOptions
{
    public const string SectionName = "JobsApi";

    /// <summary>
    /// Base URL for the Jobs API.
    /// Default for Android emulator: http://10.0.2.2:5000/
    /// Default for iOS / Mac / Localhost: http://localhost:5000/
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5000/";

    public int TimeoutSeconds { get; set; } = 8;
    public int MaxRetries { get; set; } = 3;

    public static string GetDefaultBaseUrlForPlatform()
    {
        // On Android emulator, 10.0.2.2 points to host machine loopback.
        // On iOS simulator / MacCatalyst, localhost points to host machine loopback.
#if ANDROID
        return "http://10.0.2.2:5000/";
#else
        return "http://localhost:5000/";
#endif
    }
}
