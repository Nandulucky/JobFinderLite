namespace JobFinderLite.MockApi.Middleware;

public class ChaosSettings
{
    public const string SectionName = "Chaos";

    /// <summary>
    /// Master toggle for chaos mode.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Minimum simulated latency in ms.
    /// </summary>
    public int DelayMinMs { get; set; } = 300;

    /// <summary>
    /// Maximum simulated latency in ms.
    /// </summary>
    public int DelayMaxMs { get; set; } = 800;

    /// <summary>
    /// Failure probability percentage (0.10 = 10%).
    /// </summary>
    public double FailureRate { get; set; } = 0.10;
}

public class ChaosMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ChaosSettings _settings;
    private readonly ILogger<ChaosMiddleware> _logger;
    private readonly Random _random = new();

    public ChaosMiddleware(
        RequestDelegate next,
        ChaosSettings settings,
        ILogger<ChaosMiddleware> logger)
    {
        _next = next;
        _settings = settings;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Allow header or query parameter override for explicit deterministic testing
        bool chaosActive = _settings.Enabled;
        if (context.Request.Headers.TryGetValue("X-Chaos-Mode", out var chaosHeader))
        {
            _ = bool.TryParse(chaosHeader, out chaosActive);
        }
        else if (context.Request.Query.TryGetValue("chaos", out var chaosQuery))
        {
            _ = bool.TryParse(chaosQuery, out chaosActive);
        }

        // Apply simulated realistic network delay (300ms - 800ms)
        if (_settings.DelayMaxMs > 0)
        {
            int delay = _random.Next(_settings.DelayMinMs, _settings.DelayMaxMs + 1);
            await Task.Delay(delay, context.RequestAborted);
        }

        // Inject chaos failures if enabled (~10% rate)
        if (chaosActive && _random.NextDouble() < _settings.FailureRate)
        {
            // Even split between 500 Internal Server Error and 429 Too Many Requests
            bool is500 = _random.Next(2) == 0;
            if (is500)
            {
                _logger.LogWarning("ChaosMiddleware: Injected 500 Internal Server Error for {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "ChaosMode: Simulated backend transient failure (500 Internal Server Error)",
                    timestamp = DateTimeOffset.UtcNow
                });
                return;
            }
            else
            {
                _logger.LogWarning("ChaosMiddleware: Injected 429 Too Many Requests for {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.Append("Retry-After", "1");
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "ChaosMode: Rate limit exceeded (429 Too Many Requests). Please retry after backoff.",
                    timestamp = DateTimeOffset.UtcNow
                });
                return;
            }
        }

        await _next(context);
    }
}
