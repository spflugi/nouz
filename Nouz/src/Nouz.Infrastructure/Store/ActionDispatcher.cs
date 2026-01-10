using Nouz.Application;
using Nouz.Application.Store;
using Nouz.ReduxSimple;

namespace Nouz.Infrastructure.Store;

internal sealed class ActionDispatcher : IActionDispatcher
{
    private readonly ReduxStore<RootState> _store;
    private readonly ITaskDispatcher _taskDispatcher;

    public ActionDispatcher(ReduxStore<RootState> store, ITaskDispatcher taskDispatcher)
    {
        _store = store;
        _taskDispatcher = taskDispatcher;
    }

    public Task Dispatch(IAction action)
    {
        return _taskDispatcher.Invoke(() =>
        {
            _store.Dispatch(action);
            return Task.CompletedTask;
        });
    }
}