using JobFinderLite.Application.DTOs;
using JobFinderLite.MockApi.Middleware;
using JobFinderLite.MockApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<IJobDataStore, InMemoryJobDataStore>();

var chaosSettings = new ChaosSettings();
builder.Configuration.GetSection(ChaosSettings.SectionName).Bind(chaosSettings);
builder.Services.AddSingleton(chaosSettings);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

// Apply Chaos and simulated delay middleware
app.UseMiddleware<ChaosMiddleware>();

// Endpoints
app.MapGet("/", () => Results.Ok(new
{
    service = "JobFinderLite Mock API",
    version = "1.0.0",
    docs = "/api/jobs",
    chaos = new { enabled = chaosSettings.Enabled, minDelay = chaosSettings.DelayMinMs, maxDelay = chaosSettings.DelayMaxMs }
}));

// GET /api/jobs?page=1&pageSize=20&q=&location=
app.MapGet("/api/jobs", (
    IJobDataStore store,
    int page = 1,
    int pageSize = 20,
    string? q = null,
    string? location = null) =>
{
    var (items, totalCount, hasMore) = store.GetJobs(page, pageSize, q, location);

    var dtos = items.Select(j => new JobDto
    {
        Id = j.Id,
        Title = j.Title,
        Company = j.Company,
        Location = j.Location,
        JobType = j.JobType,
        PayRate = j.PayRate,
        PostedAt = j.PostedAt,
        Description = j.Description,
        Skills = j.Skills
    }).ToList();

    var pagedResult = new PagedResult<JobDto>
    {
        Items = dtos,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        HasMore = hasMore
    };

    return Results.Ok(pagedResult);
});

// GET /api/jobs/{id}
app.MapGet("/api/jobs/{id}", (IJobDataStore store, string id) =>
{
    var job = store.GetJobById(id);
    if (job == null)
    {
        return Results.NotFound(new { error = $"Job with ID '{id}' was not found." });
    }

    var dto = new JobDto
    {
        Id = job.Id,
        Title = job.Title,
        Company = job.Company,
        Location = job.Location,
        JobType = job.JobType,
        PayRate = job.PayRate,
        PostedAt = job.PostedAt,
        Description = job.Description,
        Skills = job.Skills
    };

    return Results.Ok(dto);
});

// POST /api/applications
app.MapPost("/api/applications", (
    HttpContext context,
    IJobDataStore store,
    ApplyJobRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.JobId) ||
        string.IsNullOrWhiteSpace(request.FullName) ||
        string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest(new { error = "JobId, FullName, and Email are all required." });
    }

    // Verify job exists
    var job = store.GetJobById(request.JobId);
    if (job == null)
    {
        return Results.NotFound(new { error = $"Job '{request.JobId}' does not exist." });
    }

    // Read Idempotency-Key header
    string? idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();

    var (application, isDuplicate) = store.SubmitApplication(
        request.JobId,
        request.FullName,
        request.Email,
        idempotencyKey);

    var response = new ApplyJobResponse
    {
        Id = application.Id,
        JobId = application.JobId,
        FullName = application.FullName,
        Email = application.Email,
        AppliedAt = application.AppliedAt,
        Status = application.Status,
        IsDuplicate = isDuplicate
    };

    if (isDuplicate)
    {
        // Repeat key returns original result
        context.Response.Headers.Append("X-Idempotent-Replay", "true");
        return Results.Ok(response);
    }

    return Results.Created($"/api/applications/{application.Id}", response);
});

// Chaos Mode toggles
app.MapGet("/api/chaos", (ChaosSettings settings) => Results.Ok(settings));
app.MapPost("/api/chaos/toggle", (ChaosSettings settings, bool? enabled) =>
{
    settings.Enabled = enabled ?? !settings.Enabled;
    return Results.Ok(new { message = $"Chaos mode is now {(settings.Enabled ? "ENABLED" : "DISABLED")}", settings });
});

app.Run();
