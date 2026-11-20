using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobFinderLite.Application.DTOs;
using JobFinderLite.Application.Interfaces;

namespace JobFinderLite.Application.ViewModels;

public partial class JobListViewModel : ObservableObject, IDisposable
{
    private readonly IJobsApiClient _apiClient;
    private readonly IJobCacheService _cacheService;
    private readonly IApplicationStateService _stateService;
    private readonly INavigationService _navigationService;
    private readonly IDispatcherService _dispatcherService;

    private const int DefaultPageSize = 20;
    private CancellationTokenSource? _searchCts;
    private readonly object _pagingLock = new();

    [ObservableProperty]
    private ObservableCollection<JobCardViewModel> _jobs = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isLoadingMore;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _locationQuery = string.Empty;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private bool _hasMore = true;

    [ObservableProperty]
    private bool _isOffline;

    public JobListViewModel(
        IJobsApiClient apiClient,
        IJobCacheService cacheService,
        IApplicationStateService stateService,
        INavigationService navigationService,
        IDispatcherService? dispatcherService = null)
    {
        _apiClient = apiClient;
        _cacheService = cacheService;
        _stateService = stateService;
        _navigationService = navigationService;
        _dispatcherService = dispatcherService ?? new DefaultSyncDispatcher();

        _stateService.OnJobApplied += HandleJobApplied;
    }

    private void HandleJobApplied(string jobId)
    {
        _dispatcherService.DispatchAsync(() =>
        {
            var existing = Jobs.FirstOrDefault(j => j.Id == jobId);
            if (existing != null)
            {
                existing.IsApplied = true;
            }
        });
    }

    public void Dispose()
    {
        _stateService.OnJobApplied -= HandleJobApplied;
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = null;
    }

    partial void OnSearchQueryChanged(string value)
    {
        DebounceSearch();
    }

    partial void OnLocationQueryChanged(string value)
    {
        DebounceSearch();
    }

    private void DebounceSearch()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(400, token);
                if (!token.IsCancellationRequested)
                {
                    await LoadJobsAsync(page: 1, isRefresh: false, ct: token);
                }
            }
            catch (OperationCanceledException)
            {
                // Debounce cancelled
            }
        }, token);
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (Jobs.Count == 0)
        {
            await LoadJobsAsync(page: 1, isRefresh: false);
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsRefreshing = true;
        try
        {
            await LoadJobsAsync(page: 1, isRefresh: true);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task LoadMoreJobsAsync()
    {
        // Smooth scrolling guard: prevent duplicate loads & bounds check
        if (IsBusy || IsLoadingMore || !HasMore || HasError)
        {
            return;
        }

        lock (_pagingLock)
        {
            if (IsLoadingMore) return;
            IsLoadingMore = true;
        }

        try
        {
            int nextPage = CurrentPage + 1;
            var result = await _apiClient.GetJobsAsync(
                page: nextPage,
                pageSize: DefaultPageSize,
                q: string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim(),
                location: string.IsNullOrWhiteSpace(LocationQuery) ? null : LocationQuery.Trim());

            if (result.Items.Count > 0)
            {
                CurrentPage = nextPage;
                HasMore = result.HasMore;
                TotalCount = result.TotalCount;

                await _dispatcherService.DispatchAsync(() =>
                {
                    foreach (var item in result.Items)
                    {
                        // Guard against duplicates
                        if (!Jobs.Any(x => x.Id == item.Id))
                        {
                            Jobs.Add(new JobCardViewModel(item, _stateService, _navigationService));
                        }
                    }
                });
            }
            else
            {
                HasMore = false;
            }
        }
        catch (Exception)
        {
            // Transient error while paging - don't destroy existing list, just flag status
            ErrorMessage = "Could not load more jobs. Please check connection.";
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    [RelayCommand]
    public async Task RetryAsync()
    {
        HasError = false;
        ErrorMessage = string.Empty;
        await LoadJobsAsync(page: CurrentPage > 0 ? CurrentPage : 1, isRefresh: false);
    }

    [RelayCommand]
    public async Task SelectJobAsync(JobCardViewModel? job)
    {
        if (job == null) return;
        await _navigationService.NavigateToAsync("jobdetail", new Dictionary<string, object>
        {
            { "jobId", job.Id }
        });
    }

    public async Task LoadJobsAsync(int page = 1, bool isRefresh = false, CancellationToken ct = default)
    {
        if (page == 1)
        {
            if (!isRefresh)
            {
                IsBusy = true;
            }
            HasError = false;
            ErrorMessage = string.Empty;
        }

        try
        {
            var result = await _apiClient.GetJobsAsync(
                page: page,
                pageSize: DefaultPageSize,
                q: string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim(),
                location: string.IsNullOrWhiteSpace(LocationQuery) ? null : LocationQuery.Trim(),
                ct: ct);

            CurrentPage = page;
            TotalCount = result.TotalCount;
            HasMore = result.HasMore;

            if (page == 1)
            {
                // Cache first page for offline viewing (Bonus requirement)
                if (string.IsNullOrWhiteSpace(SearchQuery) && string.IsNullOrWhiteSpace(LocationQuery))
                {
                    await _cacheService.CacheFirstPageAsync(result);
                }
            }

            await _dispatcherService.DispatchAsync(() =>
            {
                if (page == 1)
                {
                    Jobs.Clear();
                }

                foreach (var item in result.Items)
                {
                    if (!Jobs.Any(x => x.Id == item.Id))
                    {
                        Jobs.Add(new JobCardViewModel(item, _stateService, _navigationService));
                    }
                }

                IsEmpty = Jobs.Count == 0;
            });

            IsOffline = false;
        }
        catch (OperationCanceledException)
        {
            // Debounce / user cancel
        }
        catch (Exception ex)
        {
            // If first page load fails, try offline cache fallback
            if (page == 1 && Jobs.Count == 0)
            {
                var cached = await _cacheService.GetCachedFirstPageAsync();
                if (cached != null && cached.Items.Count > 0)
                {
                    await _dispatcherService.DispatchAsync(() =>
                    {
                        Jobs.Clear();
                        foreach (var item in cached.Items)
                        {
                            Jobs.Add(new JobCardViewModel(item, _stateService, _navigationService));
                        }
                        IsEmpty = false;
                    });

                    TotalCount = cached.TotalCount;
                    HasMore = false;
                    IsOffline = true;
                    return;
                }
            }

            HasError = true;
            ErrorMessage = ex.Message.Contains("429")
                ? "Rate limit exceeded (429). Please wait a moment and tap Retry."
                : "Unable to load jobs. The server may be busy or Chaos Mode triggered. Tap Retry.";
            IsEmpty = false;
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }
}
