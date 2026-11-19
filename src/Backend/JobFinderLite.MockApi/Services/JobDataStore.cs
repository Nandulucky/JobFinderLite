using System.Collections.Concurrent;
using JobFinderLite.Domain.Entities;
using JobFinderLite.MockApi.Data;

namespace JobFinderLite.MockApi.Services;

public interface IJobDataStore
{
    (List<Job> Items, int TotalCount, bool HasMore) GetJobs(int page, int pageSize, string? q, string? location);
    Job? GetJobById(string id);
    (JobApplication Application, bool IsDuplicate) SubmitApplication(string jobId, string fullName, string email, string? idempotencyKey);
}

public class InMemoryJobDataStore : IJobDataStore
{
    private readonly List<Job> _jobs;
    private readonly ConcurrentDictionary<string, JobApplication> _applicationsByIdempotencyKey = new();
    private readonly ConcurrentBag<JobApplication> _applications = new();

    public InMemoryJobDataStore()
    {
        _jobs = JobSeeder.GenerateJobs(100);
    }

    public (List<Job> Items, int TotalCount, bool HasMore) GetJobs(int page, int pageSize, string? q, string? location)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IEnumerable<Job> query = _jobs;

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(j =>
                j.Title.ToLowerInvariant().Contains(term) ||
                j.Company.ToLowerInvariant().Contains(term) ||
                j.Description.ToLowerInvariant().Contains(term) ||
                j.Skills.Any(s => s.ToLowerInvariant().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var loc = location.Trim().ToLowerInvariant();
            query = query.Where(j => j.Location.ToLowerInvariant().Contains(loc));
        }

        var filteredList = query.ToList();
        var totalCount = filteredList.Count;
        var skip = (page - 1) * pageSize;
        var items = filteredList.Skip(skip).Take(pageSize).ToList();
        var hasMore = (skip + items.Count) < totalCount;

        return (items, totalCount, hasMore);
    }

    public Job? GetJobById(string id)
    {
        return _jobs.FirstOrDefault(j => string.Equals(j.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public (JobApplication Application, bool IsDuplicate) SubmitApplication(
        string jobId,
        string fullName,
        string email,
        string? idempotencyKey)
    {
        // Check if an application with this Idempotency-Key already exists
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            if (_applicationsByIdempotencyKey.TryGetValue(idempotencyKey, out var existing))
            {
                return (existing, true);
            }
        }

        var application = new JobApplication(jobId, fullName, email, idempotencyKey ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            // Atomically register idempotency key
            if (!_applicationsByIdempotencyKey.TryAdd(idempotencyKey, application))
            {
                var existing = _applicationsByIdempotencyKey[idempotencyKey];
                return (existing, true);
            }
        }

        _applications.Add(application);
        return (application, false);
    }
}
