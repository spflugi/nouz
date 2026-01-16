using Nouz.Application.Notebooks;

namespace Nouz.Application.Store;

public interface IState
{
    public NotebookState Notebooks { get; }
}