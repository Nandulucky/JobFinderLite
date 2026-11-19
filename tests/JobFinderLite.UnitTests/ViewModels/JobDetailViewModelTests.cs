using FluentAssertions;
using JobFinderLite.Application.DTOs;
using JobFinderLite.Application.Interfaces;
using JobFinderLite.Application.ViewModels;
using Moq;
using Xunit;

namespace JobFinderLite.UnitTests.ViewModels;

public class JobDetailViewModelTests
{
    private readonly Mock<IJobsApiClient> _mockApiClient;
    private readonly Mock<IApplicationStateService> _mockStateService;
    private readonly Mock<INavigationService> _mockNavigationService;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly JobDetailViewModel _viewModel;

    public JobDetailViewModelTests()
    {
        _mockApiClient = new Mock<IJobsApiClient>();
        _mockStateService = new Mock<IApplicationStateService>();
        _mockNavigationService = new Mock<INavigationService>();
        _mockDialogService = new Mock<IDialogService>();

        _viewModel = new JobDetailViewModel(
            _mockApiClient.Object,
            _mockStateService.Object,
            _mockNavigationService.Object,
            _mockDialogService.Object);
    }

    [Fact]
    public async Task LoadJob_WhenFound_PopulatesJobDetails()
    {
        // Arrange
        var jobDto = new JobDto
        {
            Id = "J-1042",
            Title = "Warehouse Associate",
            Company = "Acme Logistics",
            Location = "Pune, IN",
            JobType = "Temporary",
            PayRate = "₹450 / hour",
            Skills = new List<string> { "Forklift" }
        };

        _mockApiClient.Setup(x => x.GetJobByIdAsync("J-1042", It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobDto);

        // Act
        _viewModel.JobId = "J-1042";
        await _viewModel.LoadJobAsync("J-1042");

        // Assert
        _viewModel.Job.Should().NotBeNull();
        _viewModel.Job!.Title.Should().Be("Warehouse Associate");
        _viewModel.Job.Company.Should().Be("Acme Logistics");
        _viewModel.HasError.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "vinod@example.com", false, "Please enter your full name.")]
    [InlineData("Vinod Kumar", "", false, "Please enter your email address.")]
    [InlineData("Vinod Kumar", "invalid-email-string", false, "Please enter a valid email address.")]
    [InlineData("Vinod Kumar", "vinod.yadav@example.com", true, null)]
    public void ValidateForm_ValidatesCorrectly(string name, string email, bool expectedValid, string? expectedError)
    {
        // Arrange
        _viewModel.FullName = name;
        _viewModel.Email = email;

        // Act
        var isValid = _viewModel.ValidateForm();

        // Assert
        isValid.Should().Be(expectedValid);
        if (expectedError != null)
        {
            var hasMatchingError = _viewModel.FullNameError == expectedError || _viewModel.EmailError == expectedError;
            hasMatchingError.Should().BeTrue();
        }
    }

    [Fact]
    public async Task ApplyAsync_WithValidForm_SubmitsIdempotently_AndMarksJobApplied()
    {
        // Arrange
        _viewModel.JobId = "J-1042";
        _viewModel.FullName = "Vinod Kumar Yadav";
        _viewModel.Email = "vinod.yadav@example.com";

        string capturedIdempotencyKey = string.Empty;
        _mockApiClient.Setup(x => x.ApplyAsync(
                It.IsAny<ApplyJobRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<ApplyJobRequest, string, CancellationToken>((req, key, ct) => capturedIdempotencyKey = key)
            .ReturnsAsync(new ApplyJobResponse
            {
                Id = "APP-001",
                JobId = "J-1042",
                FullName = "Vinod Kumar Yadav",
                Email = "vinod.yadav@example.com",
                Status = "Submitted",
                IsDuplicate = false
            });

        // Act
        await _viewModel.ApplyAsync();

        // Assert
        _viewModel.HasApplied.Should().BeTrue();
        capturedIdempotencyKey.Should().NotBeNullOrWhiteSpace();
        _mockStateService.Verify(x => x.MarkJobApplied("J-1042"), Times.Once);
        _mockDialogService.Verify(x => x.DisplayAlertAsync("Success!", It.IsAny<string>(), "OK"), Times.Once);
    }

    [Fact]
    public async Task ApplyAsync_WhenSubmissionFails_ReusesSameIdempotencyKeyOnRetry()
    {
        // Arrange
        _viewModel.JobId = "J-1042";
        _viewModel.FullName = "Vinod Kumar Yadav";
        _viewModel.Email = "vinod.yadav@example.com";

        var idempotencyKeysUsed = new List<string>();

        // First attempt fails with transient 500
        _mockApiClient.SetupSequence(x => x.ApplyAsync(
                It.IsAny<ApplyJobRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Server error 500"))
            .ReturnsAsync(new ApplyJobResponse
            {
                Id = "APP-RETRY-01",
                JobId = "J-1042",
                FullName = "Vinod Kumar Yadav",
                Email = "vinod.yadav@example.com",
                Status = "Submitted",
                IsDuplicate = false
            });

        // Act: First attempt (fails)
        await _viewModel.ApplyAsync();
        _viewModel.HasApplied.Should().BeFalse();

        // Act: Second attempt (user retries)
        await _viewModel.ApplyAsync();

        // Assert: Succeeded on retry
        _viewModel.HasApplied.Should().BeTrue();
        _mockApiClient.Verify(x => x.ApplyAsync(
            It.Is<ApplyJobRequest>(r => r.JobId == "J-1042"),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
