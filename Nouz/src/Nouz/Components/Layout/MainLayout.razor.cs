using System.Reactive.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Nouz.Application.Settings;
using Nouz.Application.Store;

namespace Nouz.Components.Layout;

public partial class MainLayout : IDisposable
{
    private IDisposable? _themeSubscription;

    [Inject]
    private IStateProvider StateProvider { get; init; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; init; } = null!;

    protected override void OnInitialized()
    {
        _themeSubscription = StateProvider.StateObservable
            .Select(s => s.Settings.ThemeMode)
            .DistinctUntilChanged()
            .Subscribe(async themeMode =>
            {
                await ApplyTheme(themeMode);
            });
    }

    private async Task ApplyTheme(ThemeMode themeMode)
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("nouz.setTheme", themeMode == ThemeMode.Dark);
        }
        catch
        {
            // Ignore JS errors during initialization or disposal
        }
    }

    public void Dispose()
    {
        _themeSubscription?.Dispose();
    }
}
