using Microsoft.Extensions.DependencyInjection;

namespace Nouz.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterCommandsAndHandlers(this IServiceCollection services)
    {
        return services.AddMediator();
    }
}