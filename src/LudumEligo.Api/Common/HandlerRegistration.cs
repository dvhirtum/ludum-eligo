using System.Reflection;

namespace LudumEligo.Api.Common;

public static class HandlerRegistration
{
    private static readonly Type[] HandlerInterfaces = [typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)];

    /// <summary>
    /// Registers every command and query handler in <paramref name="assembly"/> as scoped,
    /// both as its concrete type (injected into endpoints) and as its handler interface.
    /// </summary>
    public static IServiceCollection AddHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }))
        {
            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && HandlerInterfaces.Contains(i.GetGenericTypeDefinition()))
                .ToList();
            if (interfaces.Count == 0)
            {
                continue;
            }

            services.AddScoped(type);
            foreach (var handlerInterface in interfaces)
            {
                services.AddScoped(handlerInterface, provider => provider.GetRequiredService(type));
            }
        }

        return services;
    }
}
