using LudumEligo.Api.Common;
using LudumEligo.Api.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace LudumEligo.Api.IntegrationTests.Common;

[Collection(ApiCollection.Name)]
public sealed class HandlerRegistrationTests(ApiFactory factory)
{
    [Fact]
    public void Should_resolve_every_handler_When_the_application_starts()
    {
        Type[] handlerInterfaces = [typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)];
        var handlerTypes = typeof(Program).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && handlerInterfaces.Contains(i.GetGenericTypeDefinition())))
            .ToList();

        using var scope = factory.Services.CreateScope();
        var unresolved = handlerTypes
            .Where(t => scope.ServiceProvider.GetService(t) is null)
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(unresolved);
    }
}
