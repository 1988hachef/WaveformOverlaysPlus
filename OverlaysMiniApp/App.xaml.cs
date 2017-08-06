using System.Windows;

namespace OverlaysMiniApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Sorry, an exception occured: " + e.Exception.Message, "OverlaysMiniApp", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
