// Program.cs
using System;
using System.Windows.Forms;
using TWS.Infrastructure;
using TWS.Infrastructure.DependencyInjection;
using TWS.Presentation;
using TWS.Services.Interfaces;
using TWS.Presentation.Forms;

namespace TWS
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Setup global exception handlers
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            ServiceContainer container = null;

            try
            {
                // Initialize Dependency Injection
                container = ServiceBootstrapper.InitializeServices();
                ServiceLocator.Initialize(container);

                var logger = ServiceLocator.GetService<ILogger>();
                logger.LogInformation("Application starting...");
                logger.LogInformation($"Version: {Application.ProductVersion}");
                logger.LogInformation($"Environment: {Environment.OSVersion}");

                // Run application
                Application.Run(new LoginForm());

                logger.LogInformation("Application exiting normally");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fatal application error:\n\n{ex.Message}\n\nThe application will now close.",
                    "Fatal Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                // Cleanup
                container?.Dispose();
            }
        }

        private static void OnThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            try
            {
                var logger = ServiceLocator.GetService<ILogger>();
                logger.LogError("Unhandled thread exception", e.Exception);
            }
            catch { }

            MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nPlease check the log file for details.",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var logger = ServiceLocator.GetService<ILogger>();
                logger.LogError("Unhandled domain exception", e.ExceptionObject as Exception);
            }
            catch { }

            if (e.IsTerminating)
            {
                MessageBox.Show(
                    "A fatal error has occurred. The application will now close.",
                    "Fatal Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}