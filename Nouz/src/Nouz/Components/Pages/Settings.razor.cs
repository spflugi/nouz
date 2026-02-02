using System.Reactive.Linq;
using Microsoft.AspNetCore.Components;
using Nouz.Application.OpenAi;
using Nouz.Application.Settings;
using Nouz.Extensions;

namespace Nouz.Components.Pages;

public partial class Settings
{
    private const int DebounceDelayMs = 800;

    private ThemeMode _themeMode = ThemeMode.Light;

    private string _openAiApiKey = string.Empty;
    private string _openAiAdminKey = string.Empty;
    private string _chatModel = string.Empty;
    private string _embeddingModel = string.Empty;
    private int _topNRelevantNotes;

    private bool _isSavingApiKey;
    private bool _isSavingApiAdminKey;
    private bool _isSavingChatModel;
    private bool _isSavingEmbeddingModel;
    private bool _isSavingTopN;

    private CancellationTokenSource? _apiKeyCts;
    private CancellationTokenSource? _apiAdminKeyCts;
    private CancellationTokenSource? _chatModelCts;
    private CancellationTokenSource? _embeddingModelCts;
    private CancellationTokenSource? _topNCts;

    // Usage section state
    private bool _isUsageExpanded;
    private bool _isLoadingUsage;
    private OpenAiUsageData? _usage;

    [Inject]
    public NavigationManager Navigation { get; init; } = null!;

    protected override void OnInitialized()
    {
        StateProvider.StateObservable
            .Select(s => s.Settings)
            .DistinctUntilChanged()
            .TakeUntilDisappearing(this)
            .Subscribe(settings =>
            {
                _themeMode = settings.ThemeMode;
                _openAiApiKey = settings.OpenAiApiKey ?? string.Empty;
                _openAiAdminKey = settings.OpenAiAdminKey ?? string.Empty;
                _chatModel = settings.OpenAiChatModel;
                _embeddingModel = settings.OpenAiEmbeddingModel;
                _topNRelevantNotes = settings.TopNRelevantNotes;
                _isLoadingUsage = settings.IsLoadingUsage;
                _usage = settings.OpenAiUsage;
                StateHasChanged();
            });
    }

    public override void Dispose()
    {
        _apiKeyCts?.Cancel();
        _apiKeyCts?.Dispose();
        _apiAdminKeyCts?.Cancel();
        _apiAdminKeyCts?.Dispose();
        _chatModelCts?.Cancel();
        _chatModelCts?.Dispose();
        _embeddingModelCts?.Cancel();
        _embeddingModelCts?.Dispose();
        _topNCts?.Cancel();
        _topNCts?.Dispose();
        base.Dispose();
    }

    private void NavigateBack()
    {
        Navigation.NavigateTo("..");
    }

    private async Task SetTheme(ThemeMode mode)
    {
        if (_themeMode != mode)
        {
            await Mediator.Send(new SettingsCommands.SaveThemeMode(mode));
        }
    }

    private async Task HandleApiKeyInput(ChangeEventArgs e)
    {
        _openAiApiKey = e.Value?.ToString() ?? string.Empty;
        _apiKeyCts = ResetCancellationToken(_apiKeyCts);
        await DebounceSaveAsync(
            _apiKeyCts.Token,
            () => _isSavingApiKey = true,
            () => _isSavingApiKey = false,
            async () => await Mediator.Send(new SettingsCommands.SaveOpenAiApiKey(_openAiApiKey)));
    }

    private async Task HandleApiAdminKeyInput(ChangeEventArgs e)
    {
        _openAiAdminKey = e.Value?.ToString() ?? string.Empty;
        _apiAdminKeyCts = ResetCancellationToken(_apiAdminKeyCts);
        await DebounceSaveAsync(
            _apiAdminKeyCts.Token,
            () => _isSavingApiAdminKey = true,
            () => _isSavingApiAdminKey = false,
            async () => await Mediator.Send(new SettingsCommands.SaveOpenAiAdminKey(_openAiAdminKey)));
    }

    private async Task HandleChatModelInput(ChangeEventArgs e)
    {
        _chatModel = e.Value?.ToString() ?? string.Empty;
        _chatModelCts = ResetCancellationToken(_chatModelCts);
        await DebounceSaveAsync(
            _chatModelCts.Token,
            () => _isSavingChatModel = true,
            () => _isSavingChatModel = false,
            async () => await Mediator.Send(new SettingsCommands.SaveOpenAiChatModel(_chatModel)));
    }

    private async Task HandleEmbeddingModelInput(ChangeEventArgs e)
    {
        _embeddingModel = e.Value?.ToString() ?? string.Empty;
        _embeddingModelCts = ResetCancellationToken(_embeddingModelCts);
        await DebounceSaveAsync(
            _embeddingModelCts.Token,
            () => _isSavingEmbeddingModel = true,
            () => _isSavingEmbeddingModel = false,
            async () => await Mediator.Send(new SettingsCommands.SaveOpenAiEmbeddingModel(_embeddingModel)));
    }

    private async Task HandleTopNInput(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var value))
        {
            _topNRelevantNotes = Math.Clamp(value, 0, 10);
        }

        _topNCts = ResetCancellationToken(_topNCts);
        await DebounceSaveAsync(
            _topNCts.Token,
            () => _isSavingTopN = true,
            () => _isSavingTopN = false,
            async () => await Mediator.Send(new SettingsCommands.SaveTopNRelevantNotes(_topNRelevantNotes)));
    }

    private static CancellationTokenSource ResetCancellationToken(CancellationTokenSource? cts)
    {
        cts?.Cancel();
        cts?.Dispose();
        return new CancellationTokenSource();
    }

    private async Task DebounceSaveAsync(
        CancellationToken token,
        Action setIsSaving,
        Action clearIsSaving,
        Func<Task> saveAction)
    {
        try
        {
            await Task.Delay(DebounceDelayMs, token);

            if (!token.IsCancellationRequested)
            {
                setIsSaving();
                StateHasChanged();

                try
                {
                    await saveAction();
                }
                finally
                {
                    clearIsSaving();
                    StateHasChanged();
                }
            }
        }
        catch (TaskCanceledException)
        {
            // Debounce was cancelled, ignore
        }
    }

    private async Task ToggleUsageExpanded()
    {
        _isUsageExpanded = !_isUsageExpanded;

        // Load usage data when expanding for the first time (or if stale)
        if (_isUsageExpanded && _usage is null && !_isLoadingUsage)
        {
            await RefreshUsage();
        }
    }

    private async Task RefreshUsage()
    {
        await Mediator.Send(new SettingsCommands.LoadOpenAiUsage());
    }

    private static string FormatNumber(long number)
    {
        return number switch
        {
            >= 1_000_000 => $"{number / 1_000_000.0:F1}M",
            >= 1_000 => $"{number / 1_000.0:F1}K",
            _ => number.ToString("N0")
        };
    }

    private static string FormatNumber(int number) => FormatNumber((long)number);

    private static string FormatTimeAgo(DateTimeOffset time)
    {
        var elapsed = DateTimeOffset.UtcNow - time;

        return elapsed.TotalMinutes switch
        {
            < 1 => "just now",
            < 60 => $"{(int)elapsed.TotalMinutes} min ago",
            < 1440 => $"{(int)elapsed.TotalHours} hr ago",
            _ => $"{(int)elapsed.TotalDays} days ago"
        };
    }
}