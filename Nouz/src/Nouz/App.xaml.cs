using Nouz.Application.Logger;

namespace Nouz
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        private readonly ILoggerAdapter<App> _logger;

        public App(ILoggerAdapter<App> logger)
        {
            _logger = logger;

            InitializeComponent();
        }

        protected override void OnStart()
        {
            _logger.LogInformation("Application is starting.");
        }

        protected override void OnResume()
        {
            _logger.LogInformation("Application is resuming.");
        }

        protected override void OnSleep()
        {
            _logger.LogInformation("Application is going to sleep.");
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "Nouz" };
        }
    }
}
