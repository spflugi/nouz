using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Nouz.Components.Modals;

public partial class TextInputModal
{
    private bool _isVisible;
    private bool _shouldFocus;
    private TaskCompletionSource<string?>? _tcs;
    private string _inputValue = string.Empty;
    private ElementReference _inputRef;

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
        _shouldFocus = true;

        StateHasChanged();
        return _tcs.Task;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_shouldFocus && _isVisible)
        {
            _shouldFocus = false;
            try
            {
                await JsRuntime.InvokeVoidAsync("nouz.focusInput", _inputRef);
            }
            catch
            {
                // Ignore JS interop errors
            }
        }
    }

    private void HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            Confirm();
        }
        else if (e.Key == "Escape")
        {
            Cancel();
        }
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