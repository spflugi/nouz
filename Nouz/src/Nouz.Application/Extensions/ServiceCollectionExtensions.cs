using Microsoft.Extensions.DependencyInjection;
using Nouz.Application.Notes;

namespace Nouz.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterCommandsAndHandlers(this IServiceCollection services)
    {
        services.AddSingleton<NoteExportService>();
        return services.AddMediator();
    }
}