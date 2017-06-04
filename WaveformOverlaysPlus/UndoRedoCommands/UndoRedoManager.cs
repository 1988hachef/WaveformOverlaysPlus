using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace WaveformOverlaysPlus.UndoRedoCommands
{
    class UndoRedoManager
    {
        interface ICommand
        {
            void Execute();
            void UnExecute();
        }

        class MoveCommand : ICommand
        {
            private double _ChangeOfTranslateX;
            private double _ChangeOfTranslateY;
            private FrameworkElement _UiElement;

            public MoveCommand(double transX, double transY, FrameworkElement uiElement)
            {
                _ChangeOfTranslateX = transX;
                _ChangeOfTranslateY = transY;
                _UiElement = uiElement;
            }

            public void Execute()
            {
                double currentX = (double)(_UiElement.RenderTransform.GetValue(CompositeTransform.TranslateXProperty));
                double currentY = (double)(_UiElement.RenderTransform.GetValue(CompositeTransform.TranslateYProperty));
                _UiElement.RenderTransform.SetValue(CompositeTransform.TranslateXProperty, (currentX + _ChangeOfTranslateX));
                _UiElement.RenderTransform.SetValue(CompositeTransform.TranslateYProperty, (currentY + _ChangeOfTranslateY));
            }

            public void UnExecute()
            {
                double currentX = (double)(_UiElement.RenderTransform.GetValue(CompositeTransform.TranslateXProperty));
                double currentY = (double)(_UiElement.RenderTransform.GetValue(CompositeTransform.TranslateYProperty));
                _UiElement.RenderTransform.SetValue(CompositeTransform.TranslateXProperty, (currentX - _ChangeOfTranslateX));
                _UiElement.RenderTransform.SetValue(CompositeTransform.TranslateYProperty, (currentY - _ChangeOfTranslateY));
            }
        }

        class ResizeCommand : ICommand
        {
            private double _ChangeOfTranslateX;
            private double _ChangeOfTranslateY;
            private double _ChangeofWidth;
            private double _Changeofheight;
            private FrameworkElement _UiElement;

            public ResizeCommand(double transX, double transY, double width, double height, FrameworkElement uiElement)
            {
                _ChangeOfTranslateX = transX;
                _ChangeOfTranslateY = transY;
                _ChangeofWidth = width;
                _Changeofheight = height;
                _UiElement = uiElement;
            }
            
            public void Execute()
            {
                double currentX = (double)(_UiElement.RenderTransform.GetValue(CompositeTransform.TranslateXProperty));
                double currentY = (double)(_UiElement.RenderTransform.GetValue(CompositeTransform.TranslateYProperty));
                _UiElement.RenderTransform.SetValue(CompositeTransform.TranslateXProperty, (currentX + _ChangeOfTranslateX));
                _UiElement.RenderTransform.SetValue(CompositeTransform.TranslateYProperty, (currentY + _ChangeOfTranslateY));
                _UiElement.Height = _UiElement.Height + _Changeofheight;
                _UiElement.Width = _UiElement.Width + _ChangeofWidth;
            }

            public void UnExecute()
            {
                double currentX = (double)(_UiElement.RenderTransform.GetValue(CompositeTransform.TranslateXProperty));
                double currentY = (double)(_UiElement.RenderTransform.GetValue(CompositeTransform.TranslateYProperty));
                _UiElement.RenderTransform.SetValue(CompositeTransform.TranslateXProperty, (currentX - _ChangeOfTranslateX));
                _UiElement.RenderTransform.SetValue(CompositeTransform.TranslateYProperty, (currentY - _ChangeOfTranslateY));
                _UiElement.Height = _UiElement.Height - _Changeofheight;
                _UiElement.Width = _UiElement.Width - _ChangeofWidth;
            }
        }

        class InsertCommand : ICommand
        {
            private FrameworkElement _UiElement;
            private Panel _Container;

            public InsertCommand(FrameworkElement uiElement, Panel container)
            {
                _UiElement = uiElement;
                _Container = container;
            }
            
            public void Execute()
            {
                if (!_Container.Children.Contains(_UiElement))
                {
                    _Container.Children.Add(_UiElement);
                }
            }

            public void UnExecute()
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

        public class UnDoRedo
        {
            private Stack<ICommand> _Undocommands = new Stack<ICommand>();
            private Stack<ICommand> _Redocommands = new Stack<ICommand>();

            public void Redo(int levels)
            {
                for (int i = 1; i <= levels; i++)
                {
                    if (_Redocommands.Count != 0)
                    {
                        ICommand command = _Redocommands.Pop();
                        command.Execute();
                        _Undocommands.Push(command);
                    }
                }
            }

            public void Undo(int levels)
            {
                for (int i = 1; i <= levels; i++)
                {
                    if (_Undocommands.Count != 0)
                    {
                        ICommand command = _Undocommands.Pop();
                        command.UnExecute();
                        _Redocommands.Push(command);
                    }
                }
            }

            public void InsertInUnDoRedoForInsert(FrameworkElement element, Panel container)
            {
                ICommand cmd = new InsertCommand(element, container);
                _Undocommands.Push(cmd);
                _Redocommands.Clear();
            }

            public void InsertInUnDoRedoForDelete(FrameworkElement element, Panel container)
            {
                ICommand cmd = new DeleteCommand(element, container);
                _Undocommands.Push(cmd);
                _Redocommands.Clear();
            }

            public void InsertInUnDoRedoForMove(double x, double y, FrameworkElement UIelement)
            {
                ICommand cmd = new MoveCommand(x, y, UIelement);
                _Undocommands.Push(cmd);
                _Redocommands.Clear();
            }

            public void InsertInUnDoRedoForResize(double x, double y, double width, double height, FrameworkElement UIelement)
            {
                ICommand cmd = new ResizeCommand(x, y, width, height, UIelement);
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
        }
    }
}
