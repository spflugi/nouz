using Nouz.Application;
using Nouz.ReduxSimple;
using System.Reactive.Concurrency;

namespace Nouz.Infrastructure.Store;

internal static class StoreFactory
{
    public static ReduxStore<RootState> Create()
    {
        IScheduler? scheduler = null;

        if (SynchronizationContext.Current is not null)
        {
            scheduler = new SynchronizationContextScheduler(SynchronizationContext.Current!, true);
        }

        var reducers = CreateReducers();
        return new ReduxStore<RootState>(reducers, RootState.InitialState, false, scheduler);
    }

    private static IEnumerable<On<RootState>> CreateReducers()
    {
        return Enumerable.Empty<On<RootState>>();
    }
}