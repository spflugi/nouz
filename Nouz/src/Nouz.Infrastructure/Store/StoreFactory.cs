using Nouz.Application;
using Nouz.ReduxSimple;
using System.Reactive.Concurrency;
using Nouz.Application.Notebooks;
using Nouz.Application.Notes;
using Nouz.Application.Notifications;

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
        return Reducers.CombineReducers(
            NotebookReducers.Create<RootState>(s => s.Notebooks),
            NoteReducers.Create<RootState>(s => s.Notes),
            NotificationReducers.Create<RootState>(s => s.Notifications));
    }
}