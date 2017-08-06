using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OverlaysMiniApp
{
    /// <summary>
    /// Interaction logic for WindowSingle.xaml
    /// </summary>
    public partial class WindowSingle : Window
    {
        public WindowSingle()
        {
            InitializeComponent();
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CommandBinding_CanExecute_1(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBinding_Executed_1(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }
        private void Window_ContentRendered_1(object sender, EventArgs e)
        {
            SolidColorBrush scb = Brushes.Black;
            Application.Current.Resources["LineColor"] = scb;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            SolidColorBrush scb = Brushes.Red;
            Application.Current.Resources["LineColor"] = scb;
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            SolidColorBrush scb = Brushes.Orange;
            Application.Current.Resources["LineColor"] = scb;
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            SolidColorBrush scb = Brushes.Yellow;
            Application.Current.Resources["LineColor"] = scb;
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            SolidColorBrush scb = Brushes.LimeGreen;
            Application.Current.Resources["LineColor"] = scb;
        }

        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            SolidColorBrush scb = Brushes.Blue;
            Application.Current.Resources["LineColor"] = scb;
        }

        private void MenuItem_Click_5(object sender, RoutedEventArgs e)
        {
            SolidColorBrush scb = Brushes.Violet;
            Application.Current.Resources["LineColor"] = scb;
        }

        private void MenuItem_Click_6(object sender, RoutedEventArgs e)
        {
            SolidColorBrush scb = Brushes.Black;
            Application.Current.Resources["LineColor"] = scb;
        }

        private void MenuItem_Click_7(object sender, RoutedEventArgs e)
        {
            SolidColorBrush scb = Brushes.Gray;
            Application.Current.Resources["LineColor"] = scb;
        }

        private void checkBox_Checked(object sender, RoutedEventArgs e)
        {
            exhaustOverlapRectangle.Visibility = Visibility.Visible;
            intakeOverlapRectangle.Visibility = Visibility.Visible;
            textBlock3.Foreground = new SolidColorBrush(Colors.Red);
            textBlock4.Foreground = new SolidColorBrush(Colors.Blue);
        }

        private void checkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            exhaustOverlapRectangle.Visibility = Visibility.Hidden;
            intakeOverlapRectangle.Visibility = Visibility.Hidden;
            textBlock3.Foreground = new SolidColorBrush(Colors.Black);
            textBlock4.Foreground = new SolidColorBrush(Colors.Black);
        }

        private void horizontalLineThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            Canvas.SetTop(horizontalLineThumb, Canvas.GetTop(horizontalLineThumb) + e.VerticalChange);
            Canvas.SetTop(horizontalLine, Canvas.GetTop(horizontalLine) + e.VerticalChange);

            if ((Canvas.GetTop(horizontalLineThumb) + 9) > backgroundCanvas.ActualHeight)
            {
                Canvas.SetTop(horizontalLineThumb, Canvas.GetTop(horizontalLineThumb) - e.VerticalChange);
                Canvas.SetTop(horizontalLine, Canvas.GetTop(horizontalLine) - e.VerticalChange);
            }

            if (Canvas.GetTop(horizontalLine) < 3)
            {
                Canvas.SetTop(horizontalLineThumb, Canvas.GetTop(horizontalLineThumb) - e.VerticalChange);
                Canvas.SetTop(horizontalLine, Canvas.GetTop(horizontalLine) - e.VerticalChange);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Canvas.SetTop(horizontalLineThumb, Canvas.GetTop(horizontalLineThumb) + (e.NewSize.Height - e.PreviousSize.Height));
            Canvas.SetTop(horizontalLine, Canvas.GetTop(horizontalLine) + (e.NewSize.Height - e.PreviousSize.Height));
            horizontalLine.Width = e.NewSize.Width;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Canvas.SetTop(horizontalLineThumb, 220);
            Canvas.SetTop(horizontalLine, 230);
        }

        private void horizontalLineThumb_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.SizeNS;
        }

        private void horizontalLineThumb_MouseLeave(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
        }
    }
}
