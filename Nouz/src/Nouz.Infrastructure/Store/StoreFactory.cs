using Nouz.Application;
using Nouz.ReduxSimple;
using System.Reactive.Concurrency;
using Nouz.Application.Attachments;
using Nouz.Application.Chat;
using Nouz.Application.Notebooks;
using Nouz.Application.Notes;
using Nouz.Application.Notifications;
using Nouz.Application.Settings;

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
            AttachmentReducers.Create<RootState>(s => s.Notes),
            NotificationReducers.Create<RootState>(s => s.Notifications),
            SettingsReducers.Create<RootState>(s => s.Settings),
            ChatReducers.Create<RootState>(s => s.Chat));
    }
}