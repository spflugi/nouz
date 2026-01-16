using Microsoft.AspNetCore.Components;

namespace Nouz.Components.Modals;

public partial class ConfirmationModal
{
    private bool _isVisible;
    private TaskCompletionSource<bool>? _tcs;

    [Parameter] 
    public string Title { get; set; } = "Confirm";

    [Parameter] 
    public string Message { get; set; } = "Are you sure?";

    [Parameter] 
    public string ConfirmText { get; set; } = "Yes";

    [Parameter] 
    public string CancelText { get; set; } = "Cancel";

    public Task<bool> Show(string title, string message, string confirmText = "Yes", string cancelText = "No")
    {
        Title = title;
        Message = message;
        ConfirmText = confirmText;
        CancelText = cancelText;

        _tcs = new TaskCompletionSource<bool>();
        _isVisible = true;

        StateHasChanged();
        return _tcs.Task;
    }

    private void Confirm()
    {
        _isVisible = false;
        _tcs?.SetResult(true);

        StateHasChanged();
    }

    private void Cancel()
    {
        _isVisible = false;
        _tcs?.SetResult(false);

        StateHasChanged();
    }
}