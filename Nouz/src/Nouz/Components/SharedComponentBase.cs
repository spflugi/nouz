using Mediator;
using Microsoft.AspNetCore.Components;
using Nouz.Application.Store;

namespace Nouz.Components;

public abstract class SharedComponentBase : ComponentBase, IDisposable
{
    public event EventHandler? DisposingEvent;

    [Inject]
    protected IMediator Mediator { get; init; } = null!;

    [Inject]
    protected IStateProvider StateProvider { get; init; } = null!;

    public virtual void Dispose()
    {
        DisposingEvent?.Invoke(this, EventArgs.Empty);
    }
}