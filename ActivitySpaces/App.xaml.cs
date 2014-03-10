
using System.Windows;
using ABC.Windows;
using ActivitySpaces.Xaml;

namespace ActivitySpaces
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            base.OnStartup(e);
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if(ActivityBar.DesktopManager !=null)
                ActivityBar.DesktopManager.Close();
        }
    }
}
