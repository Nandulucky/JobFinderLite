using JobFinderLite.Application.DTOs;

namespace JobFinderLite.Application.Interfaces;

public interface IJobCacheService
{
    Task<PagedResult<JobDto>?> GetCachedFirstPageAsync();
    Task CacheFirstPageAsync(PagedResult<JobDto> data);
    Task ClearCacheAsync();
}

public interface IApplicationStateService
{
    bool IsJobApplied(string jobId);
    void MarkJobApplied(string jobId);

    bool IsJobSaved(string jobId);
    void ToggleJobSaved(string jobId);

    event Action<string>? OnJobApplied;
    event Action<string, bool>? OnJobSavedChanged;
}

public interface INavigationService
{
    Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null);
    Task GoBackAsync();
}

public interface IDialogService
{
    Task DisplayAlertAsync(string title, string message, string cancel);
    Task<bool> DisplayConfirmationAsync(string title, string message, string accept, string cancel);
}
