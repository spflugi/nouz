using Mediator;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Nouz.Application.Store;
using Nouz.Components.Modals;

namespace Nouz.Components;

public abstract class SharedComponentBase : ComponentBase, IDisposable
{
    protected TextInputModal? TextModal = null;
    protected ConfirmationModal? ConfirmationModal = null;
    public event EventHandler? DisposingEvent;

    [Inject]
    protected IMediator Mediator { get; init; } = null!;

    [Inject]
    protected IStateProvider StateProvider { get; init; } = null!;

    [Inject]
    protected IJSRuntime JsRuntime { get; init; } = null!;

    public virtual void Dispose()
    {
        DisposingEvent?.Invoke(this, EventArgs.Empty);
    }

    protected async Task<string?> ShowTextInputModal(string title, string placeholder)
    {
        if (TextModal is null)
        {
            return null;
        }

        return await TextModal.Show(title, placeholder);
    }

    protected async Task<bool> ShowConfirmationModal(string title, string message, string confirmText = "Yes", string cancelText = "No")
    {
        if (ConfirmationModal is null)
        {
            return false;
        }

        return await ConfirmationModal.Show(title, message, confirmText, cancelText);
    }
}