using Bogus;
using JobFinderLite.Domain.Entities;

namespace JobFinderLite.MockApi.Data;

public static class JobSeeder
{
    private static readonly string[] Locations = new[]
    {
        "Pune, IN",
        "Bengaluru, IN",
        "Hyderabad, IN",
        "Mumbai, IN",
        "London, UK",
        "Manchester, UK",
        "Chicago, US",
        "New York, US",
        "Austin, US",
        "Sydney, AU"
    };

    private static readonly string[] JobTypes = new[]
    {
        "Temporary",
        "Permanent",
        "Contract"
    };

    private static readonly (string Title, string[] Skills, string PayRate)[] JobTemplates = new[]
    {
        ("Warehouse Associate", new[] { "Forklift", "Inventory Management", "Safety Compliance" }, "₹450 / hour"),
        ("Forklift Operator", new[] { "Forklift", "Loading/Unloading", "OSHA" }, "₹500 / hour"),
        ("Logistics Coordinator", new[] { "Supply Chain", "ERP", "Vendor Management" }, "₹650 / hour"),
        ("Senior .NET Developer", new[] { "C#", ".NET MAUI", "Clean Architecture", "Polly" }, "£450 / day"),
        ("Mobile App Architect", new[] { "iOS", "Android", ".NET MAUI", "System Design" }, "£650 / day"),
        ("Cloud Infrastructure Engineer", new[] { "Azure", "Terraform", "Kubernetes", "CI/CD" }, "£550 / day"),
        ("Data Engineer", new[] { "BigQuery", "Python", "SQL", "Airflow" }, "$65 / hour"),
        ("Customer Success Specialist", new[] { "CRM", "Zendesk", "Communication" }, "$28 / hour"),
        ("Healthcare Assistant", new[] { "Patient Care", "BLS", "Electronic Health Records" }, "£18 / hour"),
        ("Assembly Technician", new[] { "Electronics", "Soldering", "Quality Assurance" }, "₹380 / hour"),
        ("Quality Control Inspector", new[] { "ISO 9001", "Auditing", "Measurement Tools" }, "₹420 / hour"),
        ("Fleet Dispatcher", new[] { "Dispatching", "GPS Tracking", "Route Optimization" }, "₹550 / hour"),
        ("Site Safety Supervisor", new[] { "Risk Assessment", "First Aid", "Compliance" }, "₹600 / hour"),
        ("Front-End Developer", new[] { "React", "TypeScript", "Tailwind CSS" }, "$50 / hour"),
        ("DevOps Specialist", new[] { "Docker", "GitHub Actions", "Monitoring" }, "$70 / hour"),
    };

    public static List<Job> GenerateJobs(int count = 100)
    {
        Randomizer.Seed = new Random(42); // Deterministic seed for reproducible testing
        var jobs = new List<Job>();

        // Include the exact sample job from the assignment spec as J-1042
        jobs.Add(new Job(
            id: "J-1042",
            title: "Warehouse Associate",
            company: "Acme Logistics",
            location: "Pune, IN",
            jobType: "Temporary",
            payRate: "₹450 / hour",
            postedAt: DateTimeOffset.Parse("2026-11-15T09:30:00Z"),
            description: "We are seeking a reliable Warehouse Associate to join our fast-paced distribution facility in Pune. Key responsibilities include receiving, inspecting, and storing incoming freight, operating order pickers and forklifts safely, preparing shipments according to packing slips, and maintaining clean workstations adhering to standard safety protocols. Great hourly rate with opportunities for overtime and permanent placement.",
            skills: new[] { "Forklift", "Inventory Management", "Safety Compliance" }));

        var faker = new Faker();

        for (int i = 1; i < count; i++)
        {
            var template = JobTemplates[i % JobTemplates.Length];
            var location = Locations[faker.Random.Number(0, Locations.Length - 1)];
            var jobType = JobTypes[faker.Random.Number(0, JobTypes.Length - 1)];
            var company = faker.Company.CompanyName();
            var daysAgo = faker.Random.Number(0, 14);
            var hoursAgo = faker.Random.Number(1, 23);
            var postedAt = DateTimeOffset.UtcNow.AddDays(-daysAgo).AddHours(-hoursAgo);

            string id = $"J-{1042 + i}";
            string title = template.Title;
            if (i > 15)
            {
                title = $"{faker.Name.JobTitle()}";
            }

            string description = $"{faker.Company.CatchPhrase()}. {faker.Lorem.Paragraph(3)} Ideal candidates will have proven hands-on experience and excellent communication skills. We offer competitive remuneration, shift allowances, and clear career progression.";

            jobs.Add(new Job(
                id: id,
                title: title,
                company: company,
                location: location,
                jobType: jobType,
                payRate: template.PayRate,
                postedAt: postedAt,
                description: description,
                skills: template.Skills));
        }

        return jobs;
    }
}
