using System.Collections.ObjectModel;
using FluentAssertions;
using JobFinderLite.Application.DTOs;
using JobFinderLite.Application.Interfaces;
using JobFinderLite.Application.ViewModels;
using Moq;
using Xunit;

namespace JobFinderLite.UnitTests.ViewModels;

public class JobListViewModelTests
{
    private readonly Mock<IJobsApiClient> _mockApiClient;
    private readonly Mock<IJobCacheService> _mockCacheService;
    private readonly Mock<IApplicationStateService> _mockStateService;
    private readonly Mock<INavigationService> _mockNavigationService;
    private readonly JobListViewModel _viewModel;

    public JobListViewModelTests()
    {
        _mockApiClient = new Mock<IJobsApiClient>();
        _mockCacheService = new Mock<IJobCacheService>();
        _mockStateService = new Mock<IApplicationStateService>();
        _mockNavigationService = new Mock<INavigationService>();

        _viewModel = new JobListViewModel(
            _mockApiClient.Object,
            _mockCacheService.Object,
            _mockStateService.Object,
            _mockNavigationService.Object);
    }

    [Fact]
    public async Task Initialize_ShouldLoadFirstPage_AndSetHasMore()
    {
        // Arrange
        var fakeJobs = Enumerable.Range(1, 20).Select(i => new JobDto
        {
            Id = $"J-{1000 + i}",
            Title = $"Job {i}",
            Company = $"Company {i}",
            Location = "Pune, IN",
            JobType = "Temporary",
            PayRate = "₹450 / hour",
            PostedAt = DateTimeOffset.UtcNow
        }).ToList();

        var pagedResult = new PagedResult<JobDto>(fakeJobs, totalCount: 100, page: 1, pageSize: 20);

        _mockApiClient.Setup(x => x.GetJobsAsync(1, 20, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        _viewModel.Jobs.Should().HaveCount(20);
        _viewModel.CurrentPage.Should().Be(1);
        _viewModel.TotalCount.Should().Be(100);
        _viewModel.HasMore.Should().BeTrue();
        _viewModel.HasError.Should().BeFalse();
        _viewModel.IsEmpty.Should().BeFalse();
        _mockCacheService.Verify(x => x.CacheFirstPageAsync(pagedResult), Times.Once);
    }

    [Fact]
    public async Task LoadMoreJobs_ShouldIncrementPage_AppendItems_AndPreventDuplicates()
    {
        // Arrange: Load page 1 first
        var page1Jobs = Enumerable.Range(1, 20).Select(i => new JobDto
        {
            Id = $"J-{1000 + i}",
            Title = $"Job {i}",
            Company = $"Company {i}",
            Location = "Pune, IN",
            JobType = "Temporary",
            PayRate = "₹450 / hour"
        }).ToList();

        var page2Jobs = Enumerable.Range(21, 20).Select(i => new JobDto
        {
            Id = $"J-{1000 + i}",
            Title = $"Job {i}",
            Company = $"Company {i}",
            Location = "Pune, IN",
            JobType = "Permanent",
            PayRate = "₹600 / hour"
        }).ToList();

        _mockApiClient.Setup(x => x.GetJobsAsync(1, 20, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<JobDto>(page1Jobs, totalCount: 40, page: 1, pageSize: 20));

        _mockApiClient.Setup(x => x.GetJobsAsync(2, 20, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<JobDto>(page2Jobs, totalCount: 40, page: 2, pageSize: 20));

        await _viewModel.InitializeAsync();
        _viewModel.Jobs.Should().HaveCount(20);

        // Act: User scrolls to bottom triggering incremental load
        await _viewModel.LoadMoreJobsAsync();

        // Assert
        _viewModel.Jobs.Should().HaveCount(40);
        _viewModel.CurrentPage.Should().Be(2);
        _viewModel.HasMore.Should().BeFalse(); // Reached total count
    }

    [Fact]
    public async Task LoadJobs_WhenApiFailsAndNoCache_ShouldSetHasError_AndAllowRetry()
    {
        // Arrange
        _mockApiClient.Setup(x => x.GetJobsAsync(1, 20, null, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("ChaosMode: Injected 500 Internal Server Error", null, System.Net.HttpStatusCode.InternalServerError));

        _mockCacheService.Setup(x => x.GetCachedFirstPageAsync())
            .ReturnsAsync((PagedResult<JobDto>?)null);

        // Act
        await _viewModel.LoadJobsAsync(page: 1);

        // Assert
        _viewModel.HasError.Should().BeTrue();
        _viewModel.ErrorMessage.Should().Contain("Retry");
        _viewModel.Jobs.Should().BeEmpty();

        // Now simulate Retry succeeding
        var recoverJobs = new List<JobDto> { new() { Id = "J-1", Title = "Software Engineer", Location = "Pune, IN" } };
        _mockApiClient.Setup(x => x.GetJobsAsync(1, 20, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<JobDto>(recoverJobs, totalCount: 1, page: 1, pageSize: 20));

        await _viewModel.RetryAsync();

        _viewModel.HasError.Should().BeFalse();
        _viewModel.Jobs.Should().HaveCount(1);
    }

    [Fact]
    public async Task OfflineCacheFallback_WhenNetworkFails_ShouldShowCachedDataWithOfflineFlag()
    {
        // Arrange
        var cachedJobs = new List<JobDto>
        {
            new() { Id = "J-Offline-1", Title = "Cached Warehouse Job", Location = "Pune, IN", PayRate = "₹450 / hour" }
        };
        var cachedResult = new PagedResult<JobDto>(cachedJobs, totalCount: 1, page: 1, pageSize: 20);

        _mockApiClient.Setup(x => x.GetJobsAsync(1, 20, null, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("No network connection"));

        _mockCacheService.Setup(x => x.GetCachedFirstPageAsync())
            .ReturnsAsync(cachedResult);

        // Act
        await _viewModel.LoadJobsAsync(page: 1);

        // Assert
        _viewModel.Jobs.Should().HaveCount(1);
        _viewModel.Jobs.First().Title.Should().Be("Cached Warehouse Job");
        _viewModel.IsOffline.Should().BeTrue();
        _viewModel.HasError.Should().BeFalse();
    }
}
