namespace Nouz.Application.Store;

public interface IStateProvider
{
    /// <summary>
    /// Returns the state.
    /// </summary>
    IState State { get; }

    /// <summary>
    /// Returns the state observable.
    /// </summary>
    IObservable<IState> StateObservable { get; }
}