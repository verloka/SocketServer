using Microsoft.Extensions.DependencyInjection;
using SocketServer.BusinessLogic.Contracts;

namespace SocketServer.BusinessLogic.Extensions;

public static class IServiceCollectionExtensions
    {
    public static IServiceCollection AddBusinessLogicServices(this IServiceCollection services)
    {
        services.AddSingleton<ISessionStorage, InMemorySessionStorage>();
        services.AddSingleton<IServer, Server>();

        return services;
    }
}

