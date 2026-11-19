using System.Text.Json;
using JobFinderLite.Application.DTOs;
using JobFinderLite.Application.Interfaces;

namespace JobFinderLite.Infrastructure.Services;

public class InMemoryApplicationStateService : IApplicationStateService
{
    private readonly HashSet<string> _appliedJobs = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _savedJobs = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public event Action<string>? OnJobApplied;
    public event Action<string, bool>? OnJobSavedChanged;

    public bool IsJobApplied(string jobId)
    {
        lock (_lock)
        {
            return _appliedJobs.Contains(jobId);
        }
    }

    public void MarkJobApplied(string jobId)
    {
        lock (_lock)
        {
            _appliedJobs.Add(jobId);
        }
        OnJobApplied?.Invoke(jobId);
    }

    public bool IsJobSaved(string jobId)
    {
        lock (_lock)
        {
            return _savedJobs.Contains(jobId);
        }
    }

    public void ToggleJobSaved(string jobId)
    {
        bool isSavedNow;
        lock (_lock)
        {
            if (_savedJobs.Contains(jobId))
            {
                _savedJobs.Remove(jobId);
                isSavedNow = false;
            }
            else
            {
                _savedJobs.Add(jobId);
                isSavedNow = true;
            }
        }
        OnJobSavedChanged?.Invoke(jobId, isSavedNow);
    }
}

public class LocalJobCacheService : IJobCacheService
{
    private static PagedResult<JobDto>? _memoryCache;
    private static readonly object CacheLock = new();

    public Task<PagedResult<JobDto>?> GetCachedFirstPageAsync()
    {
        lock (CacheLock)
        {
            return Task.FromResult(_memoryCache);
        }
    }

    public Task CacheFirstPageAsync(PagedResult<JobDto> data)
    {
        lock (CacheLock)
        {
            _memoryCache = data;
        }
        return Task.CompletedTask;
    }

    public Task ClearCacheAsync()
    {
        lock (CacheLock)
        {
            _memoryCache = null;
        }
        return Task.CompletedTask;
    }
}
