using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nouz.Application.Extensions;
using Nouz.Infrastructure.Extensions;
using Radzen;

namespace Nouz;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        LoadConfiguration(builder.Configuration);

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddRadzenComponents();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        builder.Services
            .RegisterLogger(builder.Configuration, builder.Logging)
            .RegisterCommandsAndHandlers()
            .RegisterStoreAndDispatcher()
            .RegisterPersistence();

        return builder.Build();
    }

    private static void LoadConfiguration(ConfigurationManager configuration)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Nouz.appsettings.json");

        if (stream is null)
        {
            throw new InvalidOperationException("appsettings.json file not found");
        }

        var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        configuration.AddConfiguration(config);
    }
}