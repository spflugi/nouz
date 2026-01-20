using System.Reactive.Linq;
using Microsoft.AspNetCore.Components;
using Nouz.Application.Settings;
using Nouz.Extensions;

namespace Nouz.Components.Pages;

public partial class Settings
{
    private const int DebounceDelayMs = 800;

    private string _openAiApiKey = string.Empty;
    private bool _isSaving;
    private CancellationTokenSource? _debounceCts;

    [Inject]
    public NavigationManager Navigation { get; init; } = null!;

    protected override void OnInitialized()
    {
        StateProvider.StateObservable
            .Select(s => s.Settings.OpenAiApiKey ?? string.Empty)
            .DistinctUntilChanged()
            .TakeUntilDisappearing(this)
            .Subscribe(apiKey =>
            {
                _openAiApiKey = apiKey;
                StateHasChanged();
            });
    }

    public override void Dispose()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        base.Dispose();
    }

    private void NavigateBack()
    {
        Navigation.NavigateTo("..");
    }

    private async Task HandleApiKeyInput(ChangeEventArgs e)
    {
        _openAiApiKey = e.Value?.ToString() ?? string.Empty;

        // Cancel any pending save
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();

        var token = _debounceCts.Token;

        try
        {
            await Task.Delay(DebounceDelayMs, token);

            if (!token.IsCancellationRequested)
            {
                await SaveApiKey();
            }
        }
        catch (TaskCanceledException)
        {
            // Debounce was cancelled, ignore
        }
    }

    private async Task SaveApiKey()
    {
        _isSaving = true;
        StateHasChanged();

        try
        {
            await Mediator.Send(new SettingsCommands.SaveOpenAiApiKey(_openAiApiKey));
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }
}