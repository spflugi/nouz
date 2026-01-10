using Mediator;
using Nouz.Application.Lifecycle;
using Nouz.Application.Logger;

namespace Nouz
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        private readonly IMediator _mediator;
        private readonly ILoggerAdapter<App> _logger;

        private Exception? _lastFirstChanceException;

        public App(IMediator mediator, ILoggerAdapter<App> logger)
        {
            _mediator = mediator;
            _logger = logger;

            InitializeComponent();
            CatchUnhandledExceptions();
        }

        protected override async void OnStart()
        {
            _logger.LogInformation("Application is starting.");

            try
            {
                await _mediator.Send(new LifecycleCommands.PerformOnAppStart());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing application on start.");
            }
        }

        protected override async void OnResume()
        {
            _logger.LogInformation("Application is resuming.");

            try
            {
                await _mediator.Send(new LifecycleCommands.PerformOnAppResume());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing application on resume.");
            }
        }

        protected override async void OnSleep()
        {
            _logger.LogInformation("Application is going to sleep.");

            try
            {
                await _mediator.Send(new LifecycleCommands.PerformOnAppSleep());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while going to sleep.");
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "Nouz" };
        }

        private void CatchUnhandledExceptions()
        {
            TaskScheduler.UnobservedTaskException += (sender, args) => LogFatalException(args.Exception, $"Unhandled TaskScheduler exception from {sender?.ToString() ?? "Unknown"}");

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                if (args.ExceptionObject is not Exception exception)
                {
                    // ignore if exception object is not an exception
                    return;
                }

                LogFatalException(exception, $"Unhandled app domain exception from {sender?.ToString() ?? "Unknown"}");
            };

            // For WinUI 3:
            //
            // * Exceptions on background threads are caught by AppDomain.CurrentDomain.UnhandledException,
            //   not by Microsoft.UI.Xaml.Application.Current.UnhandledException
            //   See: https://github.com/microsoft/microsoft-ui-xaml/issues/5221
            //
            // * Exceptions caught by Microsoft.UI.Xaml.Application.Current.UnhandledException have details removed,
            //   but that can be worked around by saved by trapping first chance exceptions
            //   See: https://github.com/microsoft/microsoft-ui-xaml/issues/7160
            //
            AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
            {
                // First-chance exceptions are caught here very early, even before reaching a potentially
                // existing catch block. If they are not caught, they should also show up in the handler 
                // below - but that one apparently does not have the stack trace then. See here for more 
                // details: https://gist.github.com/mattjohnsonpint/7b385b7a2da7059c4a16562bc5ddb3b7
                _lastFirstChanceException = args.Exception;
            };

            Microsoft.UI.Xaml.Application.Current.UnhandledException += (sender, args) =>
            {
                var exception = args.Exception;

                if (exception.StackTrace is null && _lastFirstChanceException is not null)
                {
                    exception = _lastFirstChanceException!;
                }

                LogFatalException(exception, $"Unhandled UI Xaml exception from {sender?.ToString() ?? "Unknown"}");
            };
        }

        private void LogFatalException(Exception exception, string message)
        {
            try
            {
                _logger.LogError(exception, message);
            }
            catch
            {
                // just suppress any error logging exceptions
            }
        }
    }
}
