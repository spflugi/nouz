using Nouz.Application.Store;

namespace Nouz.Application;

public sealed record RootState : IState
{
    public static RootState InitialState { get; } = new();
}