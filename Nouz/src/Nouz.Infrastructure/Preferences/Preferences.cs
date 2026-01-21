namespace Nouz.Infrastructure.Preferences;

internal sealed class Preferences : Application.Preferences.IPreferences
{
    private readonly ISecureStorage _preferences;

    public Preferences(ISecureStorage preferences)
    {
        _preferences = preferences;
    }

    public async Task<string?> Get(string key, string? defaultValue = null)
    {
        return await _preferences.GetAsync(key).ConfigureAwait(false) ?? defaultValue;
    }

    public Task Set(string key, string value)
    {
        return _preferences.SetAsync(key, value);
    }
}