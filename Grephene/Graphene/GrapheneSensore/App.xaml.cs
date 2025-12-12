using System.Windows;
using System;
using GrapheneSensore.Services;
using GrapheneSensore.Logging;
using GrapheneSensore.Views;

namespace GrapheneSensore
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            
            try
            {
                await DatabaseInitializationService.InitializeDatabaseAsync();
                
                Logger.Instance.LogInfo("Application started successfully", "App");
                var loginWindow = new LoginWindow();
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Failed to start application", ex, "App");
                
                MessageBox.Show(
                    $"Failed to initialize application:\n\n{ex.Message}\n\nPlease check:\n" +
                    "1. SQL Server is running (MUZAMIL-WORLD\\SQLEXPRESS)\n" +
                    "2. Database 'Grephene' exists or can be created\n" +
                    "3. Connection string in appsettings.json is correct\n\n" +
                    "Check the Logs folder for more details.",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                Shutdown(1);
            }
        }
        
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Instance.LogCritical("Unhandled WPF exception", e.Exception, "App");
            
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}\n\nCheck the Logs folder for details.",
                "Application Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            e.Handled = true;
        }
        
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Logger.Instance.LogCritical("Unhandled domain exception", exception, "App");
            
            MessageBox.Show(
                $"A critical error occurred:\n\n{exception?.Message}\n\nThe application will now close.",
                "Critical Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Instance.LogInfo("Application shutting down", "App");
            base.OnExit(e);
        }
    }
}
