using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System;
using Windows.UI.Popups;
using Microsoft.Services.Store.Engagement;

namespace WaveformOverlaysPlus
{
    public sealed partial class ResetPage : Page
    {
        public ResetPage()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Frame.Navigate(typeof(MainPage));
            }
            catch (Exception ex)
            {
                await new MessageDialog("Sorry, a problem occured when resetting the page.\n\n"
                                         + ex.Message + "\n\n" + ex.StackTrace).ShowAsync();

                StoreServicesCustomEventLogger logger = StoreServicesCustomEventLogger.GetDefault();
                logger.Log("MyResetPageError" + " " + ex.Message + " " + ex.StackTrace);
            }
        }
    }
}
