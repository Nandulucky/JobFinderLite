namespace JobFinderLite.Domain.Entities;

public class Job
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string JobType { get; set; } = "Temporary"; // "Temporary", "Permanent", "Contract"
    public string PayRate { get; set; } = string.Empty; // e.g. "₹450 / hour", "£25 / hour", "$35 / hour"
    public DateTimeOffset PostedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Description { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = new();

    // Client/Session-side transient or synchronized state
    public bool IsSaved { get; set; }
    public bool IsApplied { get; set; }

    public Job()
    {
    }

    public Job(
        string id,
        string title,
        string company,
        string location,
        string jobType,
        string payRate,
        DateTimeOffset postedAt,
        string description,
        IEnumerable<string>? skills = null)
    {
        Id = id;
        Title = title;
        Company = company;
        Location = location;
        JobType = jobType;
        PayRate = payRate;
        PostedAt = postedAt;
        Description = description;
        if (skills != null)
        {
            Skills.AddRange(skills);
        }
    }
}
