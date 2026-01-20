using Nouz.ReduxSimple;

namespace Nouz.Application.Settings;

public static class SettingsReducers
{
    public static IEnumerable<On<T>> Create<T>(Func<T, SettingsState> selector) where T : class, new()
    {
        return Reducers.CreateSubReducers(selector)
            .On<SettingsActions.OpenAiApiKeyUpdated>((state, action) =>
                state with { OpenAiApiKey = action.ApiKey })
            .ToList();
    }
}
