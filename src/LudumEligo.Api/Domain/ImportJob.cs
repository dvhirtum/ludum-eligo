namespace LudumEligo.Api.Domain;

public enum ImportStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
}

/// <summary>One import of a user's BGG collection. A user has at most one queued or running import.</summary>
public sealed class ImportJob
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid UserId { get; init; }

    public ImportStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>Games added to the collection; set when the import succeeds.</summary>
    public int? Added { get; set; }

    /// <summary>Owned BGG games that were already in the collection; set when the import succeeds.</summary>
    public int? AlreadyPresent { get; set; }

    /// <summary>Why the import failed.</summary>
    public string? Error { get; set; }
}
