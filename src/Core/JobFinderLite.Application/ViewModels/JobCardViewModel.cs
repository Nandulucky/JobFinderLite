using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobFinderLite.Application.DTOs;
using JobFinderLite.Application.Interfaces;

namespace JobFinderLite.Application.ViewModels;

public partial class JobCardViewModel : ObservableObject
{
    private readonly IApplicationStateService? _stateService;
    private readonly INavigationService? _navigationService;

    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _company = string.Empty;

    [ObservableProperty]
    private string _location = string.Empty;

    [ObservableProperty]
    private string _jobType = "Temporary";

    [ObservableProperty]
    private string _payRate = string.Empty;

    [ObservableProperty]
    private DateTimeOffset _postedAt = DateTimeOffset.UtcNow;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private List<string> _skills = new();

    [ObservableProperty]
    private bool _isSaved;

    [ObservableProperty]
    private bool _isApplied;

    public string CompanyLocation => $"{Company} · {Location}";

    public string RelativePostedTime => FormatRelativeTime(PostedAt);

    public string JobTypeBgHex => JobType?.ToLowerInvariant() switch
    {
        "permanent" => "#D1FAE5", // Green tint
        "contract" => "#DBEAFE",  // Blue tint
        _ => "#FEF3C7"            // Amber tint (Temporary)
    };

    public string JobTypeColorHex => JobType?.ToLowerInvariant() switch
    {
        "permanent" => "#065F46", // Dark green
        "contract" => "#1E40AF",  // Dark blue
        _ => "#B45309"            // Dark amber (Temporary)
    };

    public JobCardViewModel() { }

    public JobCardViewModel(
        JobDto dto,
        IApplicationStateService? stateService = null,
        INavigationService? navigationService = null)
    {
        _stateService = stateService;
        _navigationService = navigationService;

        Id = dto.Id;
        Title = dto.Title;
        Company = dto.Company;
        Location = dto.Location;
        JobType = dto.JobType;
        PayRate = dto.PayRate;
        PostedAt = dto.PostedAt;
        Description = dto.Description;
        Skills = dto.Skills ?? new();

        if (_stateService != null)
        {
            IsApplied = _stateService.IsJobApplied(Id);
            IsSaved = _stateService.IsJobSaved(Id);
        }
    }

    [RelayCommand]
    public void ToggleSave()
    {
        IsSaved = !IsSaved;
        _stateService?.ToggleJobSaved(Id);
    }

    [RelayCommand]
    public async Task SelectJobAsync()
    {
        if (_navigationService != null)
        {
            await _navigationService.NavigateToAsync("jobdetail", new Dictionary<string, object>
            {
                { "jobId", Id }
            });
        }
    }

    public static string FormatRelativeTime(DateTimeOffset dateTime)
    {
        var span = DateTimeOffset.UtcNow - dateTime;
        if (span.TotalDays >= 365)
            return $"{(int)(span.TotalDays / 365)}y ago";
        if (span.TotalDays >= 30)
            return $"{(int)(span.TotalDays / 30)}mo ago";
        if (span.TotalDays >= 1)
            return $"{(int)span.TotalDays}d ago";
        if (span.TotalHours >= 1)
            return $"{(int)span.TotalHours}h ago";
        if (span.TotalMinutes >= 1)
            return $"{(int)span.TotalMinutes}m ago";

        return "Just now";
    }
}
