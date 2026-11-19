namespace JobFinderLite.Domain.Entities;

public class JobApplication
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string JobId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset AppliedAt { get; set; } = DateTimeOffset.UtcNow;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Status { get; set; } = "Submitted"; // Submitted, Reviewed, Rejected

    public JobApplication()
    {
    }

    public JobApplication(string jobId, string fullName, string email, string idempotencyKey)
    {
        JobId = jobId;
        FullName = fullName;
        Email = email;
        IdempotencyKey = idempotencyKey;
        AppliedAt = DateTimeOffset.UtcNow;
        Status = "Submitted";
    }
}
