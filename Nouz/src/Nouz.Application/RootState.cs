using Nouz.Application.Notebooks;
using Nouz.Application.Store;

namespace Nouz.Application;

public sealed record RootState : IState
{
    public static RootState InitialState { get; } = new();

    public NotebookState Notebooks { get; init; } = new();
}