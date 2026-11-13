using LudumEligo.Api.Common;
using Microsoft.Extensions.DependencyInjection;

namespace LudumEligo.Api.UnitTests.Common;

public sealed class HandlerRegistrationTests
{
    public sealed record Ping(string Message);

    public sealed class PingHandler : IQueryHandler<Ping, Result<string>>
    {
        public Task<Result<string>> HandleAsync(Ping query, CancellationToken cancellationToken) =>
            Task.FromResult<Result<string>>(query.Message);
    }

    public sealed record Rename(string Name);

    public sealed class RenameHandler : ICommandHandler<Rename, Result<bool>>
    {
        public Task<Result<bool>> HandleAsync(Rename command, CancellationToken cancellationToken) =>
            Task.FromResult<Result<bool>>(true);
    }

    [Fact]
    public void Should_register_handlers_by_interface_and_type_When_scanning_an_assembly()
    {
        var provider = new ServiceCollection()
            .AddHandlersFromAssembly(typeof(HandlerRegistrationTests).Assembly)
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        Assert.IsType<PingHandler>(scope.ServiceProvider.GetRequiredService<IQueryHandler<Ping, Result<string>>>());
        Assert.IsType<RenameHandler>(scope.ServiceProvider.GetRequiredService<ICommandHandler<Rename, Result<bool>>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<PingHandler>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<RenameHandler>());
    }
}
