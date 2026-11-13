namespace LudumEligo.Api.Domain;

public sealed class User
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    /// <summary>The Google account's stable subject identifier (<c>sub</c> claim).</summary>
    public required string GoogleSubject { get; init; }

    public required string DisplayName { get; set; }

    public required string Email { get; set; }

    public DateTimeOffset CreatedAt { get; init; }
}
