using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JobFinderLite.Application.DTOs;
using JobFinderLite.Application.Interfaces;
using JobFinderLite.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobFinderLite.Infrastructure.Services;

public class JobsApiClient : IJobsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JobsApiClient>? _logger;
    private readonly ApiOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public JobsApiClient(
        HttpClient httpClient,
        IOptions<ApiOptions>? options = null,
        ILogger<JobsApiClient>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options?.Value ?? new ApiOptions();

        if (_httpClient.BaseAddress == null && !string.IsNullOrEmpty(_options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        }
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<PagedResult<JobDto>> GetJobsAsync(
        int page = 1,
        int pageSize = 20,
        string? q = null,
        string? location = null,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(q))
            queryParams.Add($"q={Uri.EscapeDataString(q)}");

        if (!string.IsNullOrWhiteSpace(location))
            queryParams.Add($"location={Uri.EscapeDataString(location)}");

        var url = $"/api/jobs?{string.Join("&", queryParams)}";
        _logger?.LogInformation("Fetching jobs: {Url}", url);

        return await ExecuteWithResilienceAsync(async () =>
        {
            using var response = await _httpClient.GetAsync(url, ct);
            await EnsureSuccessOrThrowAsync(response);

            var stream = await response.Content.ReadAsStreamAsync(ct);
            var result = await JsonSerializer.DeserializeAsync<PagedResult<JobDto>>(stream, JsonOptions, ct);
            return result ?? new PagedResult<JobDto>();
        }, ct);
    }

    public async Task<JobDto?> GetJobByIdAsync(string id, CancellationToken ct = default)
    {
        var url = $"/api/jobs/{Uri.EscapeDataString(id)}";
        _logger?.LogInformation("Fetching job detail: {Url}", url);

        return await ExecuteWithResilienceAsync(async () =>
        {
            using var response = await _httpClient.GetAsync(url, ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            await EnsureSuccessOrThrowAsync(response);
            var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonSerializer.DeserializeAsync<JobDto>(stream, JsonOptions, ct);
        }, ct);
    }

    public async Task<ApplyJobResponse> ApplyAsync(
        ApplyJobRequest request,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        var url = "/api/applications";
        _logger?.LogInformation("Submitting application for Job: {JobId} with IdempotencyKey: {Key}", request.JobId, idempotencyKey);

        return await ExecuteWithResilienceAsync(async () =>
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                httpRequest.Headers.Add("Idempotency-Key", idempotencyKey);
            }

            var json = JsonSerializer.Serialize(request, JsonOptions);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(httpRequest, ct);
            await EnsureSuccessOrThrowAsync(response);

            var stream = await response.Content.ReadAsStreamAsync(ct);
            var result = await JsonSerializer.DeserializeAsync<ApplyJobResponse>(stream, JsonOptions, ct);
            return result ?? new ApplyJobResponse { Status = "Submitted" };
        }, ct);
    }

    /// <summary>
    /// Custom resilience loop: Handles exponential backoff for 5xx and 429 Too Many Requests.
    /// </summary>
    private async Task<T> ExecuteWithResilienceAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        int maxRetries = _options.MaxRetries;
        int delayMs = 400;

        for (int attempt = 1; attempt <= maxRetries + 1; attempt++)
        {
            try
            {
                return await action();
            }
            catch (HttpRequestException ex) when (attempt <= maxRetries && ShouldRetry(ex))
            {
                _logger?.LogWarning("Attempt {Attempt} failed with {Message}. Retrying in {Delay}ms...", attempt, ex.Message, delayMs);
                
                // If 429, wait longer (at least 800ms)
                int waitTime = ex.StatusCode == (HttpStatusCode)429 ? Math.Max(delayMs, 800) : delayMs;
                await Task.Delay(waitTime, ct);
                delayMs *= 2; // exponential backoff
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested && attempt <= maxRetries)
            {
                // Timeout occurred
                _logger?.LogWarning("Attempt {Attempt} timed out. Retrying in {Delay}ms...", attempt, delayMs);
                await Task.Delay(delayMs, ct);
                delayMs *= 2;
            }
        }

        return await action();
    }

    private static bool ShouldRetry(HttpRequestException ex)
    {
        if (ex.StatusCode == null) return true; // network transport failure
        var code = (int)ex.StatusCode.Value;
        return code == 429 || (code >= 500 && code < 600);
    }

    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = string.Empty;
            try
            {
                content = await response.Content.ReadAsStringAsync();
            }
            catch { }

            throw new HttpRequestException(
                $"API call failed with HTTP {(int)response.StatusCode} ({response.ReasonPhrase}): {content}",
                null,
                response.StatusCode);
        }
    }
}
