using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nouz.Application.Logger;
using Nouz.Application.Store;
using Nouz.Domain.Repositories;
using Nouz.Infrastructure.Logger;
using Nouz.Infrastructure.Repositories;
using Nouz.Infrastructure.Store;
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

    public static IServiceCollection RegisterStoreAndDispatcher(this IServiceCollection services)
    {
        // Add the store as a singleton so that we can register the state 
        // providers as transient. But never ever use the store directly! 

        return services
            .AddSingleton(StoreFactory.Create())
            .AddTransient<IStateProvider, StateProvider>()
            .AddTransient<ITaskDispatcher, MainThreadTaskDispatcher>()
            .AddSingleton<IActionDispatcher, ActionDispatcher>();
    }

    public static IServiceCollection RegisterDatabase(this IServiceCollection services)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(FileSystem.AppDataDirectory, "nouz.db"),
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        return services
            .AddTransient<IDbMigrator, DbMigrator>()
            .AddTransient<INotebookRepository, NotebookRepository>()
            .AddTransient<INoteRepository, NoteRepository>()
            .AddDbContextFactory<NouzDbContext>(options => { options.UseSqlite(connectionString); });
    }
}