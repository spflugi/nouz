using Nouz.Application.Store;

namespace Nouz.Infrastructure.Store;

internal sealed class MainThreadTaskDispatcher : ITaskDispatcher
{
    public Task Invoke(Func<Task> taskFactory)
    {
        var application = Microsoft.Maui.Controls.Application.Current;

        if (application is null)
        {
            throw new NotSupportedException();
        }

        return application.Dispatcher.DispatchAsync(taskFactory);
    }
}