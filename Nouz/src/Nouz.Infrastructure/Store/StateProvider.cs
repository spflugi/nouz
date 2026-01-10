using Nouz.Application;
using Nouz.Application.Store;
using Nouz.ReduxSimple;

namespace Nouz.Infrastructure.Store;

internal sealed class StateProvider : IStateProvider
{
    private readonly ReduxStore<RootState> _store;

    public IState State => _store.State;

    public IObservable<IState> StateObservable => _store.Select();

    public StateProvider(ReduxStore<RootState> store)
    {
        _store = store;
    }
}