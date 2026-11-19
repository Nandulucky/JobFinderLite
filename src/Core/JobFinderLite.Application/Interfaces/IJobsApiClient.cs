using JobFinderLite.Application.DTOs;

namespace JobFinderLite.Application.Interfaces;

public interface IJobsApiClient
{
    Task<PagedResult<JobDto>> GetJobsAsync(
        int page = 1,
        int pageSize = 20,
        string? q = null,
        string? location = null,
        CancellationToken ct = default);

    Task<JobDto?> GetJobByIdAsync(string id, CancellationToken ct = default);

    Task<ApplyJobResponse> ApplyAsync(
        ApplyJobRequest request,
        string idempotencyKey,
        CancellationToken ct = default);
}
