using Nouz.Application.Notebooks;
using Nouz.Application.Notes;
using Nouz.Application.Notifications;
using Nouz.Application.Settings;
using Nouz.Application.Store;

namespace Nouz.Application;

public sealed record RootState : IState
{
    public static RootState InitialState { get; } = new();

    public NotificationState Notifications { get; init; } = new();

    public NotebookState Notebooks { get; init; } = new();

    public NoteState Notes { get; init; } = new();

    public SettingsState Settings { get; init; } = new();
}