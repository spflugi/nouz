using Nouz.Application.Chat;
using Nouz.Application.Notebooks;
using Nouz.Application.Notes;
using Nouz.Application.Notifications;
using Nouz.Application.Settings;

namespace Nouz.Application.Store;

public interface IState
{
    public NotificationState Notifications { get; }
    public NotebookState Notebooks { get; }
    public NoteState Notes { get; }
    public SettingsState Settings { get; }
    public ChatState Chat { get; }
}