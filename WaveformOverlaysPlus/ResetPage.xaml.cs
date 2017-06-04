using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace WaveformOverlaysPlus
{
    public sealed partial class ResetPage : Page
    {
        public ResetPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }
    }
}
