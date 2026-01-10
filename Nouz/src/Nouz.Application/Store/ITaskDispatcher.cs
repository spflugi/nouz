namespace Nouz.Application.Store;

public interface ITaskDispatcher
{
    /// <summary>
    /// Invoke the given task.
    /// </summary>
    Task Invoke(Func<Task> taskFactory);
}