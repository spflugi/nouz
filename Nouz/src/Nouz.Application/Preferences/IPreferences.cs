namespace Nouz.Application.Preferences;

public interface IPreferences
{
    /// <summary>
    /// Get the value given the provided key, if it does not exist, return the defaultValue.
    /// </summary>
    string? Get(string key, string? defaultValue = null);

    /// <summary>
    /// Set a new value with the given key.
    /// </summary>
    void Set(string key, string value);
}