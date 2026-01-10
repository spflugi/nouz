namespace Nouz.Application.Store;

public interface IActionDispatcher
{
    /// <summary>
    /// Dispatch the given action to update the state.
    /// </summary>
    Task Dispatch(IAction action);
}