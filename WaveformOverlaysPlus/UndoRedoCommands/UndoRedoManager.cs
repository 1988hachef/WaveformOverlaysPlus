using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WaveformOverlaysPlus.Controls;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace WaveformOverlaysPlus.UndoRedoCommands
{
    class UndoRedoManager
    {
        public interface ICommand
        {
            void Execute();
            void UnExecute();
        }

        public class MoveCommand : ICommand
        {
            private double _ChangeOfTranslateX;
            private double _ChangeOfTranslateY;
            public FrameworkElement _UiElement;

            public MoveCommand(double changeOf_X, double changeOf_Y, FrameworkElement uiElement)
            {
                _ChangeOfTranslateX = changeOf_X;
                _ChangeOfTranslateY = changeOf_Y;
                _UiElement = uiElement;
            }

            public void Execute()
            {
                if (_ChangeOfTranslateX > 1 || _ChangeOfTranslateX < -1)
                {
                    double currentX = (double)(_UiElement.RenderTransform.GetValue(CompositeTransform.TranslateXProperty));
                    _UiElement.RenderTransform.SetValue(CompositeTransform.TranslateXProperty, (currentX + _ChangeOfTranslateX));
                }
                if (_ChangeOfTranslateY > 1 || _ChangeOfTranslateY < -1)
                {
                    double currentY = (double)(_UiElement.RenderTransform.GetValue(CompositeTransform.TranslateYProperty));
                    _UiElement.RenderTransform.SetValue(CompositeTransform.TranslateYProperty, (currentY + _ChangeOfTranslateY));
                }
            }

            public void UnExecute()
            {
                if (_ChangeOfTranslateX > 1 || _ChangeOfTranslateX < -1)
                {
                    double currentX = (double)(_UiElement.RenderTransform.GetValue(CompositeTransform.TranslateXProperty));
                    _UiElement.RenderTransform.SetValue(CompositeTransform.TranslateXProperty, (currentX - _ChangeOfTranslateX));
                }
                if (_ChangeOfTranslateY > 1 || _ChangeOfTranslateY < -1)
                {
                    double currentY = (double)(_UiElement.RenderTransform.GetValue(CompositeTransform.TranslateYProperty));
                    _UiElement.RenderTransform.SetValue(CompositeTransform.TranslateYProperty, (currentY - _ChangeOfTranslateY));
                }
            }
        }

        public class MoveOrResizeCommand : ICommand
        {
            private double _ChangeOfTranslateX;
            private double _ChangeOfTranslateY;
            private double _ChangeofWidth;
            private double _ChangeofHeight;
            public FrameworkElement _UiElement;

            public MoveOrResizeCommand(double changeOf_X, double changeOf_Y, double changeOf_width, double changeOf_height, FrameworkElement uiElement)
            {
                _ChangeOfTranslateX = changeOf_X;
                _ChangeOfTranslateY = changeOf_Y;
                _ChangeofWidth = changeOf_width;
                _ChangeofHeight = changeOf_height;
                _UiElement = uiElement;
            }
            
            public void Execute()
            {
                if (_ChangeOfTranslateX >= 1 || _ChangeOfTranslateX <= -1)
                {
                    double currentX = (double)(_UiElement.RenderTransform.GetValue(CompositeTransform.TranslateXProperty));
                    _UiElement.RenderTransform.SetValue(CompositeTransform.TranslateXProperty, (currentX + _ChangeOfTranslateX));
                }
                if (_ChangeOfTranslateY >= 1 || _ChangeOfTranslateY <= -1)
                {
                    double currentY = (double)(_UiElement.RenderTransform.GetValue(CompositeTransform.TranslateYProperty));
                    _UiElement.RenderTransform.SetValue(CompositeTransform.TranslateYProperty, (currentY + _ChangeOfTranslateY));
                }

                if (_ChangeofHeight >= 1 || _ChangeofHeight <= -1)
                {
                    _UiElement.Height = _UiElement.Height + _ChangeofHeight;
                }
                
                if (_ChangeofWidth >= 1 || _ChangeofWidth <= -1)
                {
                    _UiElement.Width = _UiElement.Width + _ChangeofWidth;
                }
            }

            public void UnExecute()
            {
                if (_ChangeOfTranslateX > 1 || _ChangeOfTranslateX < -1)
                {
                    double currentX = (double)(_UiElement.RenderTransform.GetValue(CompositeTransform.TranslateXProperty));
                    _UiElement.RenderTransform.SetValue(CompositeTransform.TranslateXProperty, (currentX - _ChangeOfTranslateX));
                }
                if (_ChangeOfTranslateY > 1 || _ChangeOfTranslateY < -1)
                {
                    double currentY = (double)(_UiElement.RenderTransform.GetValue(CompositeTransform.TranslateYProperty));
                    _UiElement.RenderTransform.SetValue(CompositeTransform.TranslateYProperty, (currentY - _ChangeOfTranslateY));
                }

                if (_ChangeofHeight >= 1 || _ChangeofHeight <= -1)
                {
                    _UiElement.Height = _UiElement.Height - _ChangeofHeight;
                }

                if (_ChangeofWidth >= 1 || _ChangeofWidth <= -1)
                {
                    _UiElement.Width = _UiElement.Width - _ChangeofWidth;
                }
            }
        }

        public class ManipOverlapCommand : ICommand
        {
            private double _ChangeOfTranslateX;
            private double _ChangeofWidth;
            private double _ChangeofHeight;
            public FrameworkElement _UiElement;
            public string _Manipulator;

            public ManipOverlapCommand(double changeOf_X, double changeOf_width, double changeOf_height, FrameworkElement uiElement, string manipulator)
            {
                _ChangeOfTranslateX = changeOf_X;
                _ChangeofWidth = changeOf_width;
                _ChangeofHeight = changeOf_height;
                _UiElement = uiElement;
                _Manipulator = manipulator;
            }

            public void Execute()
            {
                if (_Manipulator == "spEVO" || _Manipulator == "spIVO")
                {
                    if (_ChangeOfTranslateX >= 1 || _ChangeOfTranslateX <= -1)
                    {
                        double currentX = (double)(_UiElement.RenderTransform.GetValue(CompositeTransform.TranslateXProperty));
                        _UiElement.RenderTransform.SetValue(CompositeTransform.TranslateXProperty, (currentX + _ChangeOfTranslateX));
                    }

                    if (_ChangeofHeight >= 1 || _ChangeofHeight <= -1)
                    {
                        _UiElement.Height = _UiElement.Height + _ChangeofHeight;
                    }

                    if (_ChangeofWidth >= 1 || _ChangeofWidth <= -1)
                    {
                        _UiElement.Width = _UiElement.Width + _ChangeofWidth;
                    }
                }
                else if (_Manipulator == "spEVC" || _Manipulator == "spIVC")
                {
                    double currentX = (double)(_UiElement.RenderTransform.GetValue(CompositeTransform.TranslateXProperty));
                    _UiElement.RenderTransform.SetValue(CompositeTransform.TranslateXProperty, (currentX + _ChangeOfTranslateX));

                    if (_ChangeofHeight >= 1 || _ChangeofHeight <= -1)
                    {
                        _UiElement.Height = _UiElement.ActualHeight + _ChangeofHeight;
                    }

                    if (_ChangeofWidth >= 1 || _ChangeofWidth <= -1)
                    {
                        _UiElement.Width = _UiElement.ActualWidth + _ChangeofWidth;
                    }
                }
            }

            public void UnExecute()
            {
                if (_Manipulator == "spEVO" || _Manipulator == "spIVO")
                {
                    double currentX = (double)(_UiElement.RenderTransform.GetValue(CompositeTransform.TranslateXProperty));
                    _UiElement.RenderTransform.SetValue(CompositeTransform.TranslateXProperty, (currentX - _ChangeOfTranslateX));

                    if (_ChangeofHeight >= 1 || _ChangeofHeight <= -1)
                    {
                        _UiElement.Height = _UiElement.Height - _ChangeofHeight;
                    }

                    if (_ChangeofWidth >= 1 || _ChangeofWidth <= -1)
                    {
                        _UiElement.Width = _UiElement.Width - _ChangeofWidth;
                    }
                }
                else if (_Manipulator == "spEVC" || _Manipulator == "spIVC")
                {
                    if (_ChangeOfTranslateX >= 1 || _ChangeOfTranslateX <= -1)
                    {
                        double currentX = (double)(_UiElement.RenderTransform.GetValue(CompositeTransform.TranslateXProperty));
                        _UiElement.RenderTransform.SetValue(CompositeTransform.TranslateXProperty, (currentX - _ChangeOfTranslateX));
                    }

                    if (_ChangeofHeight >= 1 || _ChangeofHeight <= -1)
                    {
                        _UiElement.Height = _UiElement.ActualHeight - _ChangeofHeight;
                    }

                    if (_ChangeofWidth >= 1 || _ChangeofWidth <= -1)
                    {
                        _UiElement.Width = _UiElement.ActualWidth - _ChangeofWidth;
                    }
                }
            }
        }

        public class AddRemoveElementCommand : ICommand
        {
            public bool _AddingElement;
            public FrameworkElement _UiElement;
            private Panel _Container;

            public AddRemoveElementCommand(bool addingElement, FrameworkElement uiElement, Panel container)
            {
                _AddingElement = addingElement;
                _UiElement = uiElement;
                _Container = container;
            }
            
            public void Execute()
            {
                if (_AddingElement == true)
                {
                    Add();
                }
                else
                {
                    Remove();
                }
            }

            public void UnExecute()
            {
                if (_AddingElement == true)
                {
                    Remove();
                }
                else
                {
                    Add();
                }
            }

            private void Add()
            {
                if (!_Container.Children.Contains(_UiElement))
                {
                    _Container.Children.Add(_UiElement);
                }
            }

            private void Remove()
            {
                _Container.Children.Remove(_UiElement);
            }
        }

        class DeleteCommand : ICommand
        {
            private FrameworkElement _UiElement;
            private Panel _Container;

            public DeleteCommand(FrameworkElement uiElement, Panel container)
            {
                _UiElement = uiElement;
                _Container = container;
            }
            
            public void Execute()
            {
                _Container.Children.Remove(_UiElement);
            }

            public void UnExecute()
            {
                _Container.Children.Add(_UiElement);
            }
        }

        class DrawStrokeCommand : ICommand
        {
            private List<InkStrokeContainer> _Strokes;
            private InkStrokeContainer _Container;
            private CanvasControl _DrawingCanvas;

            public DrawStrokeCommand(List<InkStrokeContainer> strokes, InkStrokeContainer container, CanvasControl drawingCanvas)
            {
                _Strokes = strokes;
                _Container = container;
                _DrawingCanvas = drawingCanvas;
            }

            public void Execute()
            {
                _Strokes.Add(_Container);
                _DrawingCanvas.Invalidate();
            }

            public void UnExecute()
            {
                _Strokes.Remove(_Container);
                _DrawingCanvas.Invalidate();
            }
        }

        class EraseStrokeCommand : ICommand
        {
            private List<InkStrokeContainer> _Strokes;
            private InkStrokeContainer _Container;
            private CanvasControl _DrawingCanvas;

            public EraseStrokeCommand(List<InkStrokeContainer> strokes, InkStrokeContainer container, CanvasControl drawingCanvas)
            {
                _Strokes = strokes;
                _Container = container;
                _DrawingCanvas = drawingCanvas;
            }

            public void Execute()
            {
                _Strokes.Remove(_Container);
                _DrawingCanvas.Invalidate();
            }

            public void UnExecute()
            {
                _Strokes.Add(_Container);
                _DrawingCanvas.Invalidate();
            }
        }

        class InsertLineOrArrowCommand : ICommand
        {
            private Line _Line;
            private Polyline _Polyline = null;
            private Panel _Container;

            public InsertLineOrArrowCommand(Line line, Polyline polyline, Panel container)
            {
                _Line = line;
                if (polyline != null) { _Polyline = polyline; }
                _Container = container;
            }

            public void Execute()
            {
                if (!_Container.Children.Contains(_Line))
                {
                    _Container.Children.Add(_Line);

                    if (_Polyline != null)
                    {
                        _Container.Children.Add(_Polyline);
                    }
                }
            }

            public void UnExecute()
            {
                _Container.Children.Remove(_Line);

                if (_Polyline != null)
                {
                    _Container.Children.Remove(_Polyline);
                }
            }
        }

        class CropCommand : ICommand
        {
            private BitmapImage _Bitmap;
            private StorageFile _BeforeCropFile;
            private StorageFile _AfterCropFile;
            private PaintObjectTemplatedControl _PaintControl;
            private ObservableCollection<StoredImage> _ImageCollection;

            public CropCommand(BitmapImage bitmap, StorageFile beforeCropFile, StorageFile afterCropFile, PaintObjectTemplatedControl paintControl, ObservableCollection<StoredImage> imageCollection)
            {
                _Bitmap = bitmap;
                _BeforeCropFile = beforeCropFile;
                _AfterCropFile = afterCropFile;
                _PaintControl = paintControl;
                _ImageCollection = imageCollection;
            }

            public void Execute()
            {
                RemoveImageFromCollection();

                Image image = _PaintControl.Content as Image;
                image.Source = _Bitmap;
                _PaintControl.Height = _Bitmap.PixelHeight * _PaintControl.ImageScale;
                _PaintControl.Width = _Bitmap.PixelWidth * _PaintControl.ImageScale;
                _PaintControl.ImageFileName = _AfterCropFile.Name;
                _PaintControl.ImageFilePath = "ms-appdata:///local/" + _AfterCropFile.Name;

                _ImageCollection.Add(new StoredImage { FileName = _PaintControl.ImageFileName, FilePath = _PaintControl.ImageFilePath });
            }

            public async void UnExecute()
            {
                RemoveImageFromCollection();

                Image image = _PaintControl.Content as Image;
                
                using (IRandomAccessStream IRASstream = await _BeforeCropFile.OpenAsync(FileAccessMode.Read))
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(IRASstream);
                    _PaintControl.Height = bitmapImage.PixelHeight * _PaintControl.ImageScale;
                    _PaintControl.Width = bitmapImage.PixelWidth * _PaintControl.ImageScale;
                    image.Source = bitmapImage;
                }

                _PaintControl.ImageFileName = _BeforeCropFile.Name;
                _PaintControl.ImageFilePath = "ms-appdata:///local/" + _BeforeCropFile.Name;

                _ImageCollection.Add(new StoredImage { FileName = _PaintControl.ImageFileName, FilePath = _PaintControl.ImageFilePath });
            }

            void RemoveImageFromCollection()
            {
                int index = 0;
                int pos = 0;

                foreach (StoredImage storedImage in _ImageCollection)
                {
                    if (storedImage.FileName == _PaintControl.ImageFileName)
                    {
                        pos = index;
                    }
                    else
                    {
                        index++;
                    }
                }

                _ImageCollection.RemoveAt(pos);
            }
        }

        class ShowHideCompCommand : ICommand
        {
            private bool _ShowOverlay;
            private Grid _CompOverlay;
            private RadioButton _LinesColorRB;
            private RadioButton _StrokeColorRB;

            public ShowHideCompCommand(bool showOverlay, Grid compOverlay, RadioButton linesColorRB, RadioButton strokeColorRB)
            {
                _ShowOverlay = showOverlay;
                _CompOverlay = compOverlay;
                _LinesColorRB = linesColorRB;
                _StrokeColorRB = strokeColorRB;
            }

            public void Execute()
            {
                if (_ShowOverlay == true)
                {
                    Show();
                }
                else
                {
                    Hide();
                }
            }

            public void UnExecute()
            {
                if (_ShowOverlay == true)
                {
                    Hide();
                }
                else
                {
                    Show();
                }
            }

            private void Show()
            {
                _CompOverlay.Opacity = 1.0;
                _LinesColorRB.Visibility = Visibility.Visible;
                _LinesColorRB.IsChecked = true;
            }

            private void Hide()
            {
                _CompOverlay.Opacity = .001;
                _LinesColorRB.Visibility = Visibility.Collapsed;
                _StrokeColorRB.IsChecked = true;
            }
        }

        class ShowHideOverlapCommand : ICommand
        {
            private bool _ShowOverlay;
            private Grid _ExhOverlay;
            private Grid _IntOverlay;
            private TextBlock _ExhLabel;
            private TextBlock _IntLabel;

            public ShowHideOverlapCommand(bool showOverlay, Grid exhOverlay, Grid intOverlay, TextBlock exhLabel, TextBlock intLabel)
            {
                _ShowOverlay = showOverlay;
                _ExhOverlay = exhOverlay;
                _IntOverlay = intOverlay;
                _ExhLabel = exhLabel;
                _IntLabel = intLabel;
            }

            public void Execute()
            {
                if (_ShowOverlay == true)
                {
                    Show();
                }
                else
                {
                    Hide();
                }
            }

            public void UnExecute()
            {
                if (_ShowOverlay == true)
                {
                    Hide();
                }
                else
                {
                    Show();
                }
            }
            private void Show()
            {
                _IntOverlay.Visibility = Visibility.Visible;
                _ExhOverlay.Visibility = Visibility.Visible;
                _IntLabel.Foreground = new SolidColorBrush(Colors.Red);
                _ExhLabel.Foreground = new SolidColorBrush(Colors.Blue);
            }
            private void Hide()
            {
                _IntOverlay.Visibility = Visibility.Collapsed;
                _ExhOverlay.Visibility = Visibility.Collapsed;
                _IntLabel.Foreground = new SolidColorBrush(Colors.Black);
                _ExhLabel.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        public class UnDoRedo
        {
            private Stack<ICommand> _Undocommands = new Stack<ICommand>();
            private Stack<ICommand> _Redocommands = new Stack<ICommand>();

            //public event EventHandler<UndoEventArgs> UndoCompleted;
            //public event EventHandler<RedoEventArgs> RedoCompleted;

            public void Redo(int levels)
            {
                //ICommand commandThatIsRedone = null;

                for (int i = 1; i <= levels; i++)
                {
                    if (_Redocommands.Count != 0)
                    {
                        //commandThatIsRedone = _Redocommands.Peek();
                        ICommand command = _Redocommands.Pop();
                        command.Execute();
                        _Undocommands.Push(command);
                    }
                }

                //RedoCompleted.Invoke(this, new RedoEventArgs { CommandThatWasRedone = commandThatIsRedone });
            }

            public void Undo(int levels)
            {
                //ICommand commandThatIsUndone = null;

                for (int i = 1; i <= levels; i++)
                {
                    if (_Undocommands.Count != 0)
                    {
                        //commandThatIsUndone = _Undocommands.Peek();
                        ICommand command = _Undocommands.Pop();
                        command.UnExecute();
                        _Redocommands.Push(command);
                    }
                }

                //UndoCompleted.Invoke(this, new UndoEventArgs { CommandThatWasUndone = commandThatIsUndone });
            }

            public void InsertInUnDoRedoForAddRemoveElement(bool addingElement, FrameworkElement uiElement, Panel container)
            {
                ICommand cmd = new AddRemoveElementCommand(addingElement, uiElement, container);
                _Undocommands.Push(cmd);
                _Redocommands.Clear();
            }

            public void InsertInUnDoRedoForDelete(FrameworkElement element, Panel container)
            {
                ICommand cmd = new DeleteCommand(element, container);
                _Undocommands.Push(cmd);
                _Redocommands.Clear();
            }

            public void InsertInUnDoRedoForMoveOrResize(double changeOf_X, double changeOf_Y, double changeOf_width, double changeOf_height, FrameworkElement uiElement)
            {
                ICommand cmd = new MoveOrResizeCommand(changeOf_X, changeOf_Y, changeOf_width, changeOf_height, uiElement);
                _Undocommands.Push(cmd);
                _Redocommands.Clear();
            }

            public void InsertInUnDoRedoForManipOverlap(double changeOf_X, double changeOf_width, double changeOf_height, FrameworkElement uiElement, string manipulator)
            {
                ICommand cmd = new ManipOverlapCommand(changeOf_X, changeOf_width, changeOf_height, uiElement, manipulator);
                _Undocommands.Push(cmd);
                _Redocommands.Clear();
            }

            public void InsertInUnDoRedoForDrawStroke(List<InkStrokeContainer> strokes, InkStrokeContainer container, CanvasControl drawingCanvas)
            {
                ICommand cmd = new DrawStrokeCommand(strokes, container, drawingCanvas);
                _Undocommands.Push(cmd);
                _Redocommands.Clear();
            }

            public void InsertInUnDoRedoForEraseStroke(List<InkStrokeContainer> strokes, InkStrokeContainer container, CanvasControl drawingCanvas)
            {
                ICommand cmd = new EraseStrokeCommand(strokes, container, drawingCanvas);
                _Undocommands.Push(cmd);
                _Redocommands.Clear();
            }

            public void InsertInUnDoRedoForLineOrArrow(Line line, Polyline polyline, Panel container)
            {
                ICommand cmd = new InsertLineOrArrowCommand(line, polyline, container);
                _Undocommands.Push(cmd);
                _Redocommands.Clear();
            }

            public void InsertInUnDoRedoForCrop(BitmapImage bitmap, StorageFile beforeCropFile, StorageFile afterCropFile, PaintObjectTemplatedControl paintControl, ObservableCollection<StoredImage> imageCollection)
            {
                ICommand cmd = new CropCommand(bitmap, beforeCropFile, afterCropFile, paintControl, imageCollection);
                _Undocommands.Push(cmd);
                _Redocommands.Clear();
            }
            
            public void InsertInUnDoRedoForShowHideComp(bool showOverlay, Grid compOverlay, RadioButton linesColorRB, RadioButton strokeColorRB)
            {
                ICommand cmd = new ShowHideCompCommand(showOverlay, compOverlay, linesColorRB, strokeColorRB);
                _Undocommands.Push(cmd);
                _Redocommands.Clear();
            }

            public void InsertInUnDoRedoForShowHideOverlap(bool showOverlay, Grid exhOverlay, Grid intOverlay, TextBlock exhLabel, TextBlock intLabel)
            {
                ICommand cmd = new ShowHideOverlapCommand(showOverlay, exhOverlay, intOverlay, exhLabel, intLabel);
                _Undocommands.Push(cmd);
                _Redocommands.Clear();
            }

            public bool IsUndoPossible()
            {
                if (_Undocommands.Count != 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public bool IsRedoPossible()
            {
                if (_Redocommands.Count != 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public ICommand GetTopUndoCommand()
            {
                var topUndo = _Undocommands.Peek();
                return topUndo;
            }

            public ICommand GetTopRedoCommand()
            {
                var topRedo = _Redocommands.Peek();
                return topRedo;
            }

            public class UndoEventArgs : EventArgs
            {
                public ICommand CommandThatWasUndone { get; set; }
            }

            public class RedoEventArgs : EventArgs
            {
                public ICommand CommandThatWasRedone { get; set; }
            }
        }
    }
}
