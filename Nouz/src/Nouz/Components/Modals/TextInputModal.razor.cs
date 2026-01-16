using Microsoft.AspNetCore.Components;

namespace Nouz.Components.Modals;

public partial class TextInputModal
{
    private bool _isVisible;
    private TaskCompletionSource<string?>? _tcs;
    private string _inputValue = string.Empty;

    [Parameter] 
    public string Title { get; set; } = string.Empty;

    [Parameter] 
    public string Placeholder { get; set; } = string.Empty;

    public Task<string?> Show(string title, string placeholder)
    {
        Title = title;
        Placeholder = placeholder;

        _inputValue = string.Empty;
        _tcs = new TaskCompletionSource<string?>();
        _isVisible = true;

        StateHasChanged();
        return _tcs.Task;
    }

    private void Confirm()
    {
        _isVisible = false;
        _tcs?.SetResult(_inputValue);
        StateHasChanged();
    }

    private void Cancel()
    {
        _isVisible = false;
        _tcs?.SetResult(null);
        StateHasChanged();
    }
}