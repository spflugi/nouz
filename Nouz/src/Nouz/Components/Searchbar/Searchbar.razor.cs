using Microsoft.AspNetCore.Components;

namespace Nouz.Components.Searchbar;

public partial class Searchbar
{
    private string _query = string.Empty;

    private void HandleSearchInput(ChangeEventArgs e)
    {
        var value = e.Value?.ToString() ?? string.Empty;
        _query = value;
    }

    private void ClearSearchInput()
    {
        _query = string.Empty;
    }
}