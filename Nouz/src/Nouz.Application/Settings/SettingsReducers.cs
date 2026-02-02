using Nouz.ReduxSimple;

namespace Nouz.Application.Settings;

public static class SettingsReducers
{
    public static IEnumerable<On<T>> Create<T>(Func<T, SettingsState> selector) where T : class, new()
    {
        return Reducers.CreateSubReducers(selector)
            .On<SettingsActions.OpenAiApiKeyUpdated>((state, action) =>
                state with { OpenAiApiKey = action.ApiKey })
            .On<SettingsActions.OpenAiAdminKeyUpdated>((state, action) =>
                state with { OpenAiAdminKey = action.AdminKey })
            .On<SettingsActions.OpenAiChatModelUpdated>((state, action) =>
                state with { OpenAiChatModel = action.Model })
            .On<SettingsActions.OpenAiEmbeddingModelUpdated>((state, action) =>
                state with { OpenAiEmbeddingModel = action.Model })
            .On<SettingsActions.TopNRelevantNotesUpdated>((state, action) =>
                state with { TopNRelevantNotes = action.Count })
            .On<SettingsActions.OpenAiUsageLoadingStarted>((state, _) =>
                state with { IsLoadingUsage = true })
            .On<SettingsActions.OpenAiUsageLoaded>((state, action) =>
                state with { OpenAiUsage = action.Usage, IsLoadingUsage = false })
            .On<SettingsActions.ThemeModeUpdated>((state, action) =>
                state with { ThemeMode = action.Mode })
            .ToList();
    }
}
