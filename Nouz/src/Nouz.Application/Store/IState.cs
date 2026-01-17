using Nouz.Application.Notebooks;
using Nouz.Application.Notes;
using Nouz.Application.Notifications;

namespace Nouz.Application.Store;

public interface IState
{
    public NotificationState Notifications { get; }
    public NotebookState Notebooks { get; }
    public NoteState Notes { get; }
}