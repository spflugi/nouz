using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nouz.Application.Chat;
using Nouz.Application.Embeddings;
using Nouz.Application.Logger;
using Nouz.Application.OpenAi;
using Nouz.Application.Store;
using Nouz.Domain.Repositories;
using Nouz.Infrastructure.Chat;
using Nouz.Infrastructure.Chat.Plugins;
using Nouz.Infrastructure.Embeddings;
using Nouz.Infrastructure.Logger;
using Nouz.Infrastructure.OpenAi;
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

    public static IServiceCollection RegisterPersistence(this IServiceCollection services)
    {
#if DEBUG
        var dataSource = Path.Combine(FileSystem.AppDataDirectory, "dev", "nouz.db");
#else
        var dataSource = Path.Combine(FileSystem.AppDataDirectory, "nouz.db");
#endif

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dataSource,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        return services
            .AddTransient<IDbMigrator, DbMigrator>()
            .AddTransient<INotebookRepository, NotebookRepository>()
            .AddTransient<INoteRepository, NoteRepository>()
            .AddTransient<IEmbeddingRepository, EmbeddingRepository>()
            .AddTransient<IAttachmentRepository, AttachmentRepository>()
            .AddTransient<INoteAttachmentRepository, NoteAttachmentRepository>()
            .AddSingleton(SecureStorage.Default)
            .AddSingleton<Nouz.Application.Preferences.IPreferences, Nouz.Infrastructure.Preferences.Preferences>()
            .AddDbContextFactory<NouzDbContext>(options => { options.UseSqlite(connectionString); });
    }

    public static IServiceCollection RegisterChatService(this IServiceCollection services)
    {
        return services
            .AddHttpClient()
            .AddTransient<IEmbeddingService, EmbeddingService>()
            .AddTransient<INoteContextService, NoteContextService>()
            .AddTransient<INoteManagementService, NoteManagementService>()
            .AddTransient<NoteManagementPlugin>()
            .AddTransient<IChatService, ChatService>()
            .AddTransient<IOpenAiUsageService, OpenAiUsageService>();
    }
}