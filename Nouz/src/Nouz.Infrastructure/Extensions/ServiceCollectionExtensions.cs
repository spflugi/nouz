using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nouz.Application.Logger;
using Nouz.Infrastructure.Logger;
using Serilog;

namespace Nouz.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterLogger(this IServiceCollection services, IConfiguration configuration,
        ILoggingBuilder loggingBuilder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        loggingBuilder.ClearProviders();
        loggingBuilder.AddSerilog();

        return services.AddTransient(typeof(ILoggerAdapter<>), typeof(LoggerAdapter<>));
    }
}