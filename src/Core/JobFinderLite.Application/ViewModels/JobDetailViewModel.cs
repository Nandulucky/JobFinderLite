using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobFinderLite.Application.DTOs;
using JobFinderLite.Application.Interfaces;

namespace JobFinderLite.Application.ViewModels;

public partial class JobDetailViewModel : ObservableObject
{
    private readonly IJobsApiClient _apiClient;
    private readonly IApplicationStateService _stateService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [ObservableProperty]
    private string _jobId = string.Empty;

    [ObservableProperty]
    private JobCardViewModel? _job;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    // Apply Form fields
    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string? _fullNameError;

    [ObservableProperty]
    private string? _emailError;

    [ObservableProperty]
    private bool _isSubmitting;

    [ObservableProperty]
    private bool _hasApplied;

    [ObservableProperty]
    private string _applicationStatusMessage = string.Empty;

    // Stable Idempotency Key per submit attempt cycle
    private string? _currentIdempotencyKey;

    public JobDetailViewModel(
        IJobsApiClient apiClient,
        IApplicationStateService stateService,
        INavigationService navigationService,
        IDialogService dialogService)
    {
        _apiClient = apiClient;
        _stateService = stateService;
        _navigationService = navigationService;
        _dialogService = dialogService;
    }

    partial void OnJobIdChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            HasApplied = _stateService.IsJobApplied(value);
            _ = LoadJobAsync(value);
        }
    }

    [RelayCommand]
    public async Task LoadJobAsync(string? id = null)
    {
        var targetId = id ?? JobId;
        if (string.IsNullOrWhiteSpace(targetId)) return;

        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var dto = await _apiClient.GetJobByIdAsync(targetId);
            if (dto != null)
            {
                Job = new JobCardViewModel(dto, _stateService, _navigationService);
                HasApplied = _stateService.IsJobApplied(targetId);
            }
            else
            {
                HasError = true;
                ErrorMessage = $"Job {targetId} could not be found.";
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to load job details: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ApplyAsync()
    {
        // Guard against double-tap or concurrent submit
        if (IsSubmitting || HasApplied)
        {
            return;
        }

        if (!ValidateForm())
        {
            return;
        }

        IsSubmitting = true;
        ApplicationStatusMessage = string.Empty;

        // Reuse existing idempotency key if this is a retry of the same form data,
        // or generate a new one for a fresh submission.
        if (string.IsNullOrEmpty(_currentIdempotencyKey))
        {
            _currentIdempotencyKey = Guid.NewGuid().ToString("N");
        }

        var request = new ApplyJobRequest
        {
            JobId = JobId,
            FullName = FullName.Trim(),
            Email = Email.Trim()
        };

        try
        {
            var response = await _apiClient.ApplyAsync(request, _currentIdempotencyKey);

            HasApplied = true;
            if (Job != null)
            {
                Job.IsApplied = true;
            }
            _stateService.MarkJobApplied(JobId);

            ApplicationStatusMessage = response.IsDuplicate
                ? "Your application was already received and confirmed."
                : "Application submitted successfully!";

            await _dialogService.DisplayAlertAsync(
                "Success!",
                $"Application for {Job?.Title ?? "this position"} at {Job?.Company ?? "the company"} has been submitted.\n\nStatus: {response.Status}",
                "OK");

            // Reset key on confirmed success
            _currentIdempotencyKey = null;
        }
        catch (Exception ex)
        {
            // Do NOT reset _currentIdempotencyKey here!
            // When user taps retry, the SAME key will be sent, ensuring idempotency.
            var isRateLimit = ex.Message.Contains("429");
            var message = isRateLimit
                ? "Server is busy (429 Rate Limit). Tap Apply again to retry safely."
                : $"Submission error: {ex.Message}. You can safely tap Apply again to retry.";

            await _dialogService.DisplayAlertAsync("Submission Failed", message, "OK");
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        await _navigationService.GoBackAsync();
    }

    public bool ValidateForm()
    {
        bool isValid = true;
        FullNameError = null;
        EmailError = null;

        if (string.IsNullOrWhiteSpace(FullName))
        {
            FullNameError = "Please enter your full name.";
            isValid = false;
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            EmailError = "Please enter your email address.";
            isValid = false;
        }
        else if (!EmailRegex.IsMatch(Email.Trim()))
        {
            EmailError = "Please enter a valid email address.";
            isValid = false;
        }

        return isValid;
    }
}
