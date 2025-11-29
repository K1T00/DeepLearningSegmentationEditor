using Microsoft.Extensions.DependencyInjection;

namespace AnnotationTool.App
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetColorMode(SystemColorMode.System);

            // Bootstrap DI container
            var serviceProvider = ServiceRegistration.ConfigureServices();
            var mainForm = serviceProvider.GetRequiredService<MainForm>();

            // Alternative way using HostBuilder (if needed in future after complete .NET project update)
            //using var host = ServiceRegistration.BuildHost();
            //var mainForm = host.Services.GetRequiredService<MainForm>();

            Application.Run(mainForm);
        }
    }
}