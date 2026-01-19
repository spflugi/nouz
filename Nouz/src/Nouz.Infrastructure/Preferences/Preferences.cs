namespace Nouz.Infrastructure.Preferences;

internal sealed class Preferences : Application.Preferences.IPreferences
{
    private readonly IPreferences _preferences;

    public Preferences(IPreferences preferences)
    {
        _preferences = preferences;
    }

    public string? Get(string key, string? defaultValue = null)
    {
        return _preferences.Get(key, defaultValue);
    }

    public void Set(string key, string value)
    {
        _preferences.Set(key, value);
    }
}