namespace LudumEligo.Api.IntegrationTests.Infrastructure;

/// <summary>Unique values so tests sharing one database never collide.</summary>
public static class TestData
{
    private static int _bggId = 1_000_000;

    public static int NextBggId() => Interlocked.Increment(ref _bggId);
}
