namespace JobFinderLite.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class JobNotFoundException : DomainException
{
    public string JobId { get; }
    public JobNotFoundException(string jobId) : base($"Job with ID '{jobId}' was not found.")
    {
        JobId = jobId;
    }
}

public class ValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string field, string error)
        : base($"Validation failed: {error}")
    {
        Errors = new Dictionary<string, string[]> { [field] = new[] { error } };
    }

    public ValidationException(Dictionary<string, string[]> errors)
        : base("One or more validation failures have occurred.")
    {
        Errors = errors;
    }
}
