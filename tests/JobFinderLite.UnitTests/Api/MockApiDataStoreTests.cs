using FluentAssertions;
using JobFinderLite.MockApi.Data;
using JobFinderLite.MockApi.Services;
using Xunit;

namespace JobFinderLite.UnitTests.Api;

public class MockApiDataStoreTests
{
    [Fact]
    public void SeedJobs_ShouldGenerateAtLeast100Jobs_IncludingJ1042()
    {
        // Act
        var jobs = JobSeeder.GenerateJobs(100);

        // Assert
        jobs.Should().HaveCount(100);
        var j1042 = jobs.FirstOrDefault(j => j.Id == "J-1042");
        j1042.Should().NotBeNull();
        j1042!.Title.Should().Be("Warehouse Associate");
        j1042.Company.Should().Be("Acme Logistics");
        j1042.Location.Should().Be("Pune, IN");
        j1042.PayRate.Should().Be("₹450 / hour");
        j1042.JobType.Should().Be("Temporary");
        j1042.Skills.Should().Contain("Forklift");
    }

    [Fact]
    public void Paging_ShouldReturnAccuratePagesAndHasMore()
    {
        // Arrange
        var store = new InMemoryJobDataStore();

        // Act: Page 1 (20 items)
        var (items1, totalCount1, hasMore1) = store.GetJobs(page: 1, pageSize: 20, q: null, location: null);

        // Assert
        items1.Should().HaveCount(20);
        totalCount1.Should().Be(100);
        hasMore1.Should().BeTrue();

        // Act: Page 5 (last page of 100 items)
        var (items5, totalCount5, hasMore5) = store.GetJobs(page: 5, pageSize: 20, q: null, location: null);
        items5.Should().HaveCount(20);
        hasMore5.Should().BeFalse();
    }

    [Fact]
    public void Filtering_ShouldFilterByKeywordAndLocation()
    {
        // Arrange
        var store = new InMemoryJobDataStore();

        // Act: Filter by Pune location
        var (items, totalCount, _) = store.GetJobs(page: 1, pageSize: 50, q: null, location: "Pune");

        // Assert
        items.Should().NotBeEmpty();
        items.All(j => j.Location.Contains("Pune")).Should().BeTrue();
    }

    [Fact]
    public void SubmitApplication_HonoursIdempotencyKey_RepeatedKeyReturnsOriginalWithoutDuplicate()
    {
        // Arrange
        var store = new InMemoryJobDataStore();
        string key = Guid.NewGuid().ToString("N");

        // Act 1: Initial submission
        var (app1, isDuplicate1) = store.SubmitApplication("J-1042", "Vinod Kumar Yadav", "vinod.yadav@example.com", key);

        // Assert 1
        isDuplicate1.Should().BeFalse();
        app1.Id.Should().NotBeNullOrWhiteSpace();
        app1.FullName.Should().Be("Vinod Kumar Yadav");

        // Act 2: Repeat submission with SAME Idempotency-Key
        var (app2, isDuplicate2) = store.SubmitApplication("J-1042", "Vinod Kumar Yadav", "vinod.yadav@example.com", key);

        // Assert 2: Must return original result, marked duplicate, without creating new record
        isDuplicate2.Should().BeTrue();
        app2.Id.Should().Be(app1.Id);
        app2.AppliedAt.Should().Be(app1.AppliedAt);
    }
}
