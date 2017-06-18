using System;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace WaveformOverlaysPlus.Controls
{
    public sealed class PaintObjectTemplatedControl : ContentControl
    {
        public PaintObjectTemplatedControl()
        {
            this.DefaultStyleKey = typeof(PaintObjectTemplatedControl);
        }

        public CompositeTransform transform_myControl;
        CompositeTransform transform_rectForFlyout;
        Button _closeButton;
        Grid _myWindow;
        StackPanel _spanelOpacity;
        ContentPresenter _contentPresenter;
        Rectangle _rectForFlyoutPosition;
        Rectangle _rectRight;
        Rectangle _rectTopLeft;
        Rectangle _rectTop;
        Rectangle _rectTopRight;
        Rectangle _rectLeft;
        Rectangle _rectBottomLeft;
        Rectangle _rectBottom;
        Rectangle _rectBottomRight;
        Slider _sliderOpacity;
        MenuFlyoutItem _menuBringToFront;
        MenuFlyoutItem _menuSendToBack;

        bool ContentIsTextbox = false;
        int minHeight = 40;
        int minWidth = 40;

        #region Dependency Properties

        public double ImageScale
        {
            get { return (double)GetValue(ImageScaleProperty); }
            set { SetValue(ImageScaleProperty, value); }
        }
        public static readonly DependencyProperty ImageScaleProperty =
            DependencyProperty.Register("ImageScale", typeof(double), typeof(PaintObjectTemplatedControl), new PropertyMetadata(1.0));


        public bool IsForCropper
        {
            get { return (bool)GetValue(IsForCropperProperty); }
            set { SetValue(IsForCropperProperty, value); }
        }
        public static readonly DependencyProperty IsForCropperProperty =
            DependencyProperty.Register("IsForCropper", typeof(bool), typeof(PaintObjectTemplatedControl), new PropertyMetadata(false));


        public string ImageFilePath
        {
            get { return (string)GetValue(ImageFilePathProperty); }
            set { SetValue(ImageFilePathProperty, value); }
        }
        public static readonly DependencyProperty ImageFilePathProperty =
            DependencyProperty.Register("ImageFilePath", typeof(string), typeof(PaintObjectTemplatedControl), new PropertyMetadata(""));


        public string ImageFileName
        {
            get { return (string)GetValue(ImageFileNameProperty); }
            set { SetValue(ImageFileNameProperty, value); }
        }
        public static readonly DependencyProperty ImageFileNameProperty =
            DependencyProperty.Register("ImageFileName", typeof(string), typeof(PaintObjectTemplatedControl), new PropertyMetadata(""));


        public bool OpacitySliderIsVisible
        {
            get { return (bool)GetValue(OpacitySliderIsVisibleProperty); }
            set { SetValue(OpacitySliderIsVisibleProperty, value); }
        }
        public static readonly DependencyProperty OpacitySliderIsVisibleProperty =
            DependencyProperty.Register("OpacitySliderIsVisible", typeof(bool), typeof(PaintObjectTemplatedControl), new PropertyMetadata(false));

        #endregion

        protected override void OnApplyTemplate()
        {
            _menuBringToFront = GeneralizedGetTemplateChild<MenuFlyoutItem>("menuBringToFront");
            _menuSendToBack = GeneralizedGetTemplateChild<MenuFlyoutItem>("menuSendToBack");
            _sliderOpacity = GeneralizedGetTemplateChild<Slider>("sliderOpacity");
            _closeButton = GeneralizedGetTemplateChild<Button>("btnClose");
            _myWindow = GeneralizedGetTemplateChild<Grid>("myWindow");
            _spanelOpacity = GeneralizedGetTemplateChild<StackPanel>("spanelOpacity");
            _contentPresenter = GeneralizedGetTemplateChild<ContentPresenter>("ContentPresenter");
            _rectRight = GeneralizedGetTemplateChild<Rectangle>("Right");
            _rectTopLeft = GeneralizedGetTemplateChild<Rectangle>("TopLeft");
            _rectTop = GeneralizedGetTemplateChild<Rectangle>("Top");
            _rectTopRight = GeneralizedGetTemplateChild<Rectangle>("TopRight");
            _rectLeft = GeneralizedGetTemplateChild<Rectangle>("Left");
            _rectBottomLeft = GeneralizedGetTemplateChild<Rectangle>("BottomLeft");
            _rectBottom = GeneralizedGetTemplateChild<Rectangle>("Bottom");
            _rectBottomRight = GeneralizedGetTemplateChild<Rectangle>("BottomRight");
            _rectForFlyoutPosition = GeneralizedGetTemplateChild<Rectangle>("rectForFlyoutPosition");

            this.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            _contentPresenter.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            _myWindow.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            _rectRight.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            _rectTopLeft.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            _rectTop.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            _rectTopRight.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            _rectLeft.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            _rectBottomLeft.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            _rectBottom.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            _rectBottomRight.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            _rectForFlyoutPosition.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;

            this.Loaded += TemplatedWindowControl_Loaded;
            this.SizeChanged += PaintObjectTemplatedControl_SizeChanged;

            _contentPresenter.ManipulationDelta += _contentPresenter_ManipulationDelta;
            _contentPresenter.PointerEntered += _contentPresenter_PointerEntered;
            _contentPresenter.PointerExited += _contentPresenter_PointerExited;

            _closeButton.Click += _closeButton_Click;

            _menuBringToFront.Click += _menuBringToFront_Click;
            _menuSendToBack.Click += _menuSendToBack_Click;

            _myWindow.PointerEntered += _myWindow_PointerEntered;
            _myWindow.PointerExited += _myWindow_PointerExited;
            _myWindow.RightTapped += _myWindow_RightTapped;

            _rectRight.ManipulationDelta += _rectRight_ManipulationDelta;
            _rectRight.PointerEntered += _PointerEntered;
            _rectRight.PointerExited += _PointerExited;

            _rectTopLeft.ManipulationDelta += _rectTopLeft_ManipulationDelta;
            _rectTopLeft.PointerEntered += _PointerEntered;
            _rectTopLeft.PointerExited += _PointerExited;

            _rectTop.ManipulationDelta += _rectTop_ManipulationDelta;
            _rectTop.PointerEntered += _PointerEntered;
            _rectTop.PointerExited += _PointerExited;

            _rectTopRight.ManipulationDelta += _rectTopRight_ManipulationDelta;
            _rectTopRight.PointerEntered += _PointerEntered;
            _rectTopRight.PointerExited += _PointerExited;

            _rectLeft.ManipulationDelta += _rectLeft_ManipulationDelta;
            _rectLeft.PointerEntered += _PointerEntered;
            _rectLeft.PointerExited += _PointerExited;

            _rectBottomLeft.ManipulationDelta += _rectBottomLeft_ManipulationDelta;
            _rectBottomLeft.PointerEntered += _PointerEntered;
            _rectBottomLeft.PointerExited += _PointerExited;

            _rectBottom.ManipulationDelta += _rectBottom_ManipulationDelta;
            _rectBottom.PointerEntered += _PointerEntered;
            _rectBottom.PointerExited += _PointerExited;

            _rectBottomRight.ManipulationDelta += _rectBottomRight_ManipulationDelta;
            _rectBottomRight.PointerEntered += _PointerEntered;
            _rectBottomRight.PointerExited += _PointerExited;

            transform_myControl = new CompositeTransform();
            this.RenderTransform = transform_myControl;

            transform_rectForFlyout = new CompositeTransform();
            _rectForFlyoutPosition.RenderTransform = transform_rectForFlyout;
        }

        public event EventHandler Closing;

        public event EventHandler<Z_Change_EventArgs> Z_Order_Changed;

        public class Z_Change_EventArgs : EventArgs
        {
            public int ChangeOfZ { get; set; }
        }

        private void PaintObjectTemplatedControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var jail = (Panel)this.Parent;

            //Get top left point
            GeneralTransform gt = this.TransformToVisual(jail);
            Point TopLeftPoint = gt.TransformPoint(new Point(0, 0));

            // Set these variables to represent the edges of "this"
            double left = TopLeftPoint.X;
            double top = TopLeftPoint.Y;
            double right = left + this.ActualWidth;
            double bottom = top + this.ActualHeight;

            // Reposition "this" to keep inside panel
            if (left < 0)
            {
                transform_myControl.TranslateX = 0;
            }
            else if ((right > jail.ActualWidth) && (left > 0))
            {
                double updatedLeft = jail.ActualWidth - this.ActualWidth;
                transform_myControl.TranslateX = updatedLeft - this.BorderThickness.Right;
            }

            if (top < 0)
            {
                transform_myControl.TranslateY = 0;
            }
            else if ((bottom > jail.ActualHeight) && (top > 0))
            {
                double updatedTop = jail.ActualHeight - this.ActualHeight;
                transform_myControl.TranslateY = updatedTop - this.BorderThickness.Bottom;
            }
        }

        private void TemplatedWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Set Zindex
            Panel parent = ((Panel)this.Parent);
            FrameworkElement myElement = this;
            int myZ = Canvas.GetZIndex(myElement);
            int ZWeAreChecking = 0;
            int maxZ = 0;
            for (int i = 0; i < parent.Children.Count; i++)
            {
                FrameworkElement childWeAreChecking = parent.Children[i] as FrameworkElement;
                ZWeAreChecking = Canvas.GetZIndex(childWeAreChecking);
                if (maxZ < ZWeAreChecking)
                {
                    maxZ = ZWeAreChecking;
                }
            }
            Canvas.SetZIndex(myElement, maxZ + 1);

            // Get initial opacity setting
            double initialOpacity = this.Opacity;

            // Reset opacity to 1.0
            this.Opacity = 1.0;

            // Set slider to initial opacity
            if (initialOpacity < 0.25)
            {
                initialOpacity = 0.25;
            }
            _sliderOpacity.Value = initialOpacity * 100;

            // Set the binding between the opacity slider and the opacity of _contentPresenter
            Binding binding = new Binding();
            binding.Source = _sliderOpacity;
            binding.Path = new PropertyPath("Value");
            binding.Mode = BindingMode.TwoWay;
            binding.Converter = new Converters.OpacityBindingConverter();
            _contentPresenter.SetBinding(OpacityProperty, binding);

            // Check if content is TextBox
            if (this.Content != null)
            {
                if (this.Content.GetType() == typeof(TextBox))
                {
                    ContentIsTextbox = true;
                }
            }
        }

        private void _closeButton_Click(object sender, RoutedEventArgs e)
        {
            if (Closing != null)
            {
                Closing.Invoke(this, null);
            }

            ((Panel)this.Parent).Children.Remove(this);
        }

        private void _myWindow_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            Point pointerPosition = e.GetPosition(this);
            transform_rectForFlyout.TranslateX = pointerPosition.X;
            transform_rectForFlyout.TranslateY = pointerPosition.Y;

            FlyoutBase flyout = FlyoutBase.GetAttachedFlyout(_myWindow);
            flyout.Placement = FlyoutPlacementMode.Right;
            flyout.ShowAt(_rectForFlyoutPosition);

            e.Handled = true;

            //FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private void _menuBringToFront_Click(object sender, RoutedEventArgs e)
        {
            Panel parent = ((Panel)this.Parent);
            UIElement myElement = this;
            int myZ = Canvas.GetZIndex(myElement);
            int ZWeAreChecking = 0;
            int maxZ = 0;

            for (int i = 0; i < parent.Children.Count; i++)
            {
                UIElement childWeAreChecking = parent.Children[i] as UIElement;
                ZWeAreChecking = Canvas.GetZIndex(childWeAreChecking);
                if (maxZ < ZWeAreChecking)
                {
                    maxZ = ZWeAreChecking;
                }
            }

            if (myZ < maxZ)
            {
                var newZ = maxZ + 1;
                Canvas.SetZIndex(myElement, newZ);

                if (Z_Order_Changed != null)
                {
                    var changeOfZ = newZ - myZ;
                    Z_Order_Changed.Invoke(this, new Z_Change_EventArgs { ChangeOfZ = changeOfZ });
                }
            }
        }

        private void _menuSendToBack_Click(object sender, RoutedEventArgs e)
        {
            Panel parent = ((Panel)this.Parent);
            UIElement myElement = this;
            int myZ = Canvas.GetZIndex(myElement);
            int ZWeAreChecking = 0;
            int minZ = 0;

            for (int i = 0; i < parent.Children.Count; i++)
            {
                UIElement childWeAreChecking = parent.Children[i] as UIElement;
                ZWeAreChecking = Canvas.GetZIndex(childWeAreChecking);
                if (minZ > ZWeAreChecking)
                {
                    minZ = ZWeAreChecking;
                }
            }

            if (myZ > minZ)
            {
                var newZ = minZ - 1;
                Canvas.SetZIndex(myElement, newZ);
                
                if (Z_Order_Changed != null)
                {
                    var changeOfZ = newZ - myZ;
                    Z_Order_Changed.Invoke(this, new Z_Change_EventArgs { ChangeOfZ = changeOfZ });
                }
            }
        }

        #region Pointer Enter/Exit events

        private void _myWindow_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (_closeButton.Visibility == Visibility.Visible)
            {
                _myWindow.BorderBrush = new SolidColorBrush(Colors.Transparent);
                _closeButton.Visibility = Visibility.Collapsed;
            }

            if (_spanelOpacity.Visibility == Visibility.Visible)
            {
                _spanelOpacity.Visibility = Visibility.Collapsed;
            }
        }

        private void _myWindow_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (IsForCropper == false)
            {
                _myWindow.BorderBrush = GetColorFromHex("#B2007ACC");
                _closeButton.Visibility = Visibility.Visible;
            }
            if (OpacitySliderIsVisible == true)
            {
                _spanelOpacity.Visibility = Visibility.Visible;
            }
        }

        private void _PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (ContentIsTextbox == true)
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.SizeAll, 1);
            }
            else
            {
                string name = (sender as Rectangle).Name;

                switch (name)
                {
                    case "BottomRight":
                        Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.SizeNorthwestSoutheast, 1);
                        break;
                    case "Right":
                        Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.SizeWestEast, 1);
                        break;
                    case "Bottom":
                        Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.SizeNorthSouth, 1);
                        break;
                    case "Top":
                        Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.SizeNorthSouth, 1);
                        break;
                    case "Left":
                        Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.SizeWestEast, 1);
                        break;
                    case "TopLeft":
                        Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.SizeNorthwestSoutheast, 1);
                        break;
                    case "TopRight":
                        Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.SizeNortheastSouthwest, 1);
                        break;
                    case "BottomLeft":
                        Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.SizeNortheastSouthwest, 1);
                        break;
                }
            }
        }

        private void _PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
        }

        private void _contentPresenter_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
        }

        private void _contentPresenter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (ContentIsTextbox == false)
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.SizeAll, 1);
            }
        }

        #endregion

        #region ManipulationDelta handlers

        private void _contentPresenter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (ContentIsTextbox == false)
            {
                MoveAndRestrain(e);
            }
        }

        private void _rectBottomRight_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (ContentIsTextbox == true)
            {
                MoveAndRestrain(e);
            }
            else
            {
                // Get top left point
                GeneralTransform gt = this.TransformToVisual((Panel)this.Parent);
                Point TopLeftPoint = gt.TransformPoint(new Point(0, 0));

                // Set these to represent the edges
                double right = TopLeftPoint.X + this.ActualWidth;
                double bottom = TopLeftPoint.Y + this.ActualHeight;

                // Combine the adjustable edges with the movement value.
                double rightAdjust = right + e.Delta.Translation.X;
                double bottomAdjust = bottom + e.Delta.Translation.Y;

                // Set these to use for restricting the minimum size
                double yadjust = this.ActualHeight + e.Delta.Translation.Y;
                double xadjust = this.ActualWidth + e.Delta.Translation.X;

                // Allow adjustment within restrictions
                if ((rightAdjust <= ((Panel)this.Parent).ActualWidth) && (xadjust >= minWidth))
                {
                    this.Width = xadjust;
                }

                if ((bottomAdjust <= ((Panel)this.Parent).ActualHeight) && (yadjust >= minHeight))
                {
                    this.Height = yadjust;
                }
            }
        }

        private void _rectBottom_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (ContentIsTextbox == true)
            {
                MoveAndRestrain(e);
            }
            else
            {
                //Get top left point
                GeneralTransform gt = this.TransformToVisual((Panel)this.Parent);
                Point TopLeftPoint = gt.TransformPoint(new Point(0, 0));

                // Set this to represent the bottom edge
                double bottom = TopLeftPoint.Y + this.ActualHeight;

                // Combine the bottom edge with the movement value
                double bottomAdjust = bottom + e.Delta.Translation.Y;

                // Set this to use for restricting the minimum size
                double yadjust = this.ActualHeight + e.Delta.Translation.Y;

                // Allow adjustment within restrictions
                if ((bottomAdjust <= ((Panel)this.Parent).ActualHeight) && (yadjust >= minHeight))
                {
                    this.Height = yadjust;
                }
            }
        }

        private void _rectBottomLeft_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (ContentIsTextbox == true)
            {
                MoveAndRestrain(e);
            }
            else
            {
                // Get top left point
                GeneralTransform gt = this.TransformToVisual((Panel)this.Parent);
                Point TopLeftPoint = gt.TransformPoint(new Point(0, 0));

                // Set these to represent the edges
                double left = TopLeftPoint.X;
                double bottom = TopLeftPoint.Y + this.ActualHeight;

                // Combine the adjustable edges with the movement value
                double leftAdjust = left + e.Delta.Translation.X;
                double bottomAdjust = bottom + e.Delta.Translation.Y;

                // Set these to use for restricting the minimum size
                double yadjust = this.ActualHeight + e.Delta.Translation.Y;
                double xadjust = this.ActualWidth - e.Delta.Translation.X;

                // Allow adjustment within restrictions
                if ((leftAdjust >= 0) && (xadjust >= minWidth))
                {
                    transform_myControl.TranslateX += e.Delta.Translation.X;
                    this.Width = xadjust;
                }

                if ((bottomAdjust <= ((Panel)this.Parent).ActualHeight) && (yadjust >= minHeight))
                {
                    this.Height = yadjust;
                }
            }
        }

        private void _rectLeft_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (ContentIsTextbox == true)
            {
                MoveAndRestrain(e);
            }
            else
            {
                //Get top left point
                GeneralTransform gt = this.TransformToVisual((Panel)this.Parent);
                Point TopLeftPoint = gt.TransformPoint(new Point(0, 0));

                // Set this to represent the left edge
                double left = TopLeftPoint.X;

                // Combine the left edge with the movement value
                double leftAdjust = left + e.Delta.Translation.X;

                // Set this variable to use for restricting the minimum size
                double xadjust = this.ActualWidth - e.Delta.Translation.X;

                // Allow adjustment within restrictions
                if ((leftAdjust >= 0) && (xadjust >= minWidth))
                {
                    transform_myControl.TranslateX += e.Delta.Translation.X;
                    this.Width = this.ActualWidth - e.Delta.Translation.X;
                }
            }
        }

        private void _rectTopRight_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (ContentIsTextbox == true)
            {
                MoveAndRestrain(e);
            }
            else
            {
                // Get top left point
                GeneralTransform gt = this.TransformToVisual((Panel)this.Parent);
                Point TopLeftPoint = gt.TransformPoint(new Point(0, 0));

                // Set these variables to represent the edges
                double right = TopLeftPoint.X + this.ActualWidth;
                double top = TopLeftPoint.Y;

                // Combine the adjustable edges with the movement value
                double rightAdjust = right + e.Delta.Translation.X;
                double topAdjust = top + e.Delta.Translation.Y;

                // Set these to use for restricting the minimum size
                double yadjust = this.ActualHeight - e.Delta.Translation.Y;
                double xadjust = this.ActualWidth + e.Delta.Translation.X;

                // Allow adjustment within restrictions
                if ((rightAdjust <= ((Panel)this.Parent).ActualWidth) && (xadjust >= minWidth))
                {
                    this.Width = xadjust;
                }

                if ((topAdjust >= 0) && (yadjust >= minHeight))
                {
                    transform_myControl.TranslateY += e.Delta.Translation.Y;
                    this.Height = yadjust;
                }
            }
        }

        private void _rectTop_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (ContentIsTextbox == true)
            {
                MoveAndRestrain(e);
            }
            else
            {
                //Get top left point
                GeneralTransform gt = this.TransformToVisual((Panel)this.Parent);
                Point TopLeftPoint = gt.TransformPoint(new Point(0, 0));

                // Set this to represent the top edge
                double top = TopLeftPoint.Y;

                // Combine the top edge with the movement value
                double topAdjust = top + e.Delta.Translation.Y;

                // Set this variable to use for restricting the minimum size
                double yadjust = this.ActualHeight - e.Delta.Translation.Y;

                // Allow adjustment within restrictions
                if ((topAdjust >= 0) && (yadjust >= minHeight))
                {
                    transform_myControl.TranslateY += e.Delta.Translation.Y;
                    this.Height = this.ActualHeight - e.Delta.Translation.Y;
                }
            }
        }

        private void _rectTopLeft_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (ContentIsTextbox == true)
            {
                MoveAndRestrain(e);
            }
            else
            {
                // Get top left point
                GeneralTransform gt = this.TransformToVisual((Panel)this.Parent);
                Point TopLeftPoint = gt.TransformPoint(new Point(0, 0));

                // Set these to represent the edges
                double left = TopLeftPoint.X;
                double top = TopLeftPoint.Y;

                // Combine the adjustable edges with the movement value
                double leftAdjust = left + e.Delta.Translation.X;
                double topAdjust = top + e.Delta.Translation.Y;

                // Set these to use for restricting the minimum size
                double yadjust = this.ActualHeight - e.Delta.Translation.Y;
                double xadjust = this.ActualWidth - e.Delta.Translation.X;

                // Allow adjustment within restrictions
                if ((leftAdjust >= 0) && (xadjust >= minWidth))
                {
                    transform_myControl.TranslateX += e.Delta.Translation.X;
                    this.Width = xadjust;
                }

                if ((topAdjust >= 0) && (yadjust >= minHeight))
                {
                    transform_myControl.TranslateY += e.Delta.Translation.Y;
                    this.Height = yadjust;
                }
            }
        }

        private void _rectRight_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (ContentIsTextbox == true)
            {
                MoveAndRestrain(e);
            }
            else
            {
                //Get top left point
                GeneralTransform gt = this.TransformToVisual((Panel)this.Parent);
                Point TopLeftPoint = gt.TransformPoint(new Point(0, 0));

                // Set this variable to represent the right edge of myCustomWindow
                double right = TopLeftPoint.X + this.ActualWidth;

                // Combine the right edge with the movement value
                double rightAdjust = right + e.Delta.Translation.X;

                // Set this variable to use for restricting the minimum size
                double xadjust = this.ActualWidth + e.Delta.Translation.X;

                // Allow adjustment within restrictions
                if ((rightAdjust <= ((Panel)this.Parent).ActualWidth) && (xadjust >= minWidth))
                {
                    this.Width = xadjust;
                }
            }
        }

        private void MoveAndRestrain(ManipulationDeltaRoutedEventArgs e)
        {
            // Get the top left point
            GeneralTransform gt = this.TransformToVisual((Panel)this.Parent);
            Point TopLeftPoint = gt.TransformPoint(new Point(0, 0));

            // Set these variables to represent the edges
            double left = TopLeftPoint.X;
            double top = TopLeftPoint.Y;
            double right = left + this.ActualWidth;
            double bottom = top + this.ActualHeight;

            // Combine those edges with the movement value (This prevents the moveable object from getting stuck at boundary)
            double leftAdjust = left + e.Delta.Translation.X;
            double topAdjust = top + e.Delta.Translation.Y;
            double rightAdjust = right + e.Delta.Translation.X;
            double bottomAdjust = bottom + e.Delta.Translation.Y;

            // Allow movement if within boundary (Use two separate "if" statements here, so the movement isn't sticky at the boundary)
            if ((leftAdjust >= 0) && (rightAdjust <= ((Panel)this.Parent).ActualWidth))
            {
                transform_myControl.TranslateX += e.Delta.Translation.X;
            }

            if ((topAdjust >= 0) && (bottomAdjust <= ((Panel)this.Parent).ActualHeight))
            {
                transform_myControl.TranslateY += e.Delta.Translation.Y;
            }
        }

        #endregion

        childElement GeneralizedGetTemplateChild<childElement>(string name) where childElement : DependencyObject
        {
            childElement child = GetTemplateChild(name) as childElement;

            if (child == null)
            {
                throw new NullReferenceException(name);
            }

            return child;
        }

        private SolidColorBrush GetColorFromHex(string hexColor)
        {
            return new SolidColorBrush(
                Color.FromArgb(Convert.ToByte(hexColor.Substring(1, 2), 16),
                               Convert.ToByte(hexColor.Substring(3, 2), 16),
                               Convert.ToByte(hexColor.Substring(5, 2), 16),
                               Convert.ToByte(hexColor.Substring(7, 2), 16)));
        }
    }
}
