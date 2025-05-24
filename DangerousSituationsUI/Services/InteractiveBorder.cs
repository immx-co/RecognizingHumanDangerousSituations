using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using System;

namespace DangerousSituationsUI.Services
{
    public class InteractiveBorder : Border
    {
        private bool _isPressed;
        private bool _isResizing;
        private bool _isDragging;
        private Point _topLeft;
        private Point _initialSize;
        private Point _positionInBlock;
        private int _image_height;
        private int _image_width;
        private int _image_offset;
        private TranslateTransform _transform = null!;

        public event EventHandler<BorderMovedEventArgs> BorderMoved;
        public event EventHandler<BorderResizedEventArgs> BorderResized;
        public bool IsPressed
        {
            get => _isPressed;
            set => _isPressed = value;
        }
        public Point TopLeft
        {
            get => _topLeft;
            set
            {
                _topLeft = value;
                Canvas.SetLeft(this, value.X);
                Canvas.SetTop(this, value.Y);
            }
        }
        public int ImageHeight
        {
            get => _image_height;
            set => _image_height = value;
        }
        public int ImageWidth
        {
            get => _image_width;
            set => _image_width = value;
        }
        public int ImageOffset
        {
            get => _image_offset;
            set => _image_offset = value;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            IsPressed = true;

            var parent = Parent as Canvas;
            if (parent != null)
                _positionInBlock = e.GetPosition(parent) - new Point(Canvas.GetLeft(this), Canvas.GetTop(this));

            if (e.Source is Rectangle rect)
            {
                _isResizing = true;
                _initialSize = new Point(Width, Height);
            }
            else
            {
                _isDragging = true;
            }

            base.OnPointerPressed(e);
        }
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            IsPressed = false;
            _isResizing = false;
            _isDragging = false;

            if (_transform != null && BorderMoved != null)
            {
                BorderMoved(this, new BorderMovedEventArgs
                {
                    OffsetX = TopLeft.X + _transform.X,
                    OffsetY = TopLeft.Y + _transform.Y,
                });
            }

            base.OnPointerReleased(e);
        }
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (!IsPressed)
                return;

            if (Parent == null)
                return;

            if (_isResizing)
            {
                var parent = Parent as Canvas;
                var pointerOnCanvas = e.GetPosition(parent);
                var handle = e.Source as Rectangle;

                const int minWidth = 20;
                const int minHeight = 20;

                double left = Canvas.GetLeft(this); // координата X левого края рамки
                double top = Canvas.GetTop(this);   // координата Y левого края рамки
                double right = left + Width;        // координата X правого края рамки
                double bottom = top + Height;       // координата Y правого края рамки

                double newLeft = left, newTop = top, newRight = right, newBottom = bottom;

                switch (handle.Name)
                {
                    case "BottomRight":
                        newRight = Math.Min(pointerOnCanvas.X, ImageOffset + ImageWidth);
                        newBottom = Math.Min(pointerOnCanvas.Y, ImageHeight);
                        break;
                    case "BottomLeft":
                        newLeft = Math.Max(pointerOnCanvas.X, ImageOffset);
                        newBottom = Math.Min(pointerOnCanvas.Y, ImageHeight);
                        break;
                    case "TopRight":
                        newRight = Math.Min(pointerOnCanvas.X, ImageOffset + ImageWidth);
                        newTop = Math.Max(pointerOnCanvas.Y, 0);
                        break;
                    case "TopLeft":
                        newLeft = Math.Max(pointerOnCanvas.X, ImageOffset);
                        newTop = Math.Max(pointerOnCanvas.Y, 0);
                        break;
                }

                // проверка новой ширины рамки
                if (newRight - newLeft < minWidth)
                {
                    // корректируем, чтобы была не меньше допустимой (minWidth)
                    if (handle.Name == "TopLeft" || handle.Name == "BottomLeft")
                        newLeft = newRight - minWidth;
                    else
                        newRight = newLeft + minWidth;
                }
                // проверка новой высоты рамки
                if (newBottom - newTop < minHeight)
                {
                    // корректируем, чтобы была не меньше допустимой (minHeight)
                    if (handle.Name == "TopLeft" || handle.Name == "TopRight")
                        newTop = newBottom - minHeight;
                    else
                        newBottom = newTop + minHeight;
                }

                newLeft = Math.Max(newLeft, ImageOffset);
                newRight = Math.Min(newRight, ImageOffset + ImageWidth);
                newTop = Math.Max(newTop, 0);
                newBottom = Math.Min(newBottom, ImageHeight);

                double width = newRight - newLeft;
                double height = newBottom - newTop;

                Canvas.SetLeft(this, newLeft);
                Canvas.SetTop(this, newTop);
                Width = width;
                Height = height;

                BorderResized?.Invoke(this, new BorderResizedEventArgs
                {
                    NewWidth = width,
                    NewHeight = height,
                    NewX = newLeft,
                    NewY = newTop
                });
            }

            if (_isDragging)
            {
                var parent = Parent as Canvas;
                var pointerOnCanvas = e.GetPosition(parent);

                double offsetX = _positionInBlock.X;
                double offsetY = _positionInBlock.Y;

                double newLeft = pointerOnCanvas.X - offsetX;
                double newTop = pointerOnCanvas.Y - offsetY;

                newLeft = Math.Max(ImageOffset, Math.Min(newLeft, ImageOffset + ImageWidth - Width));
                newTop = Math.Max(0, Math.Min(newTop, ImageHeight - Height));

                Canvas.SetLeft(this, newLeft);
                Canvas.SetTop(this, newTop);

                BorderMoved?.Invoke(this, new BorderMovedEventArgs
                {
                    OffsetX = newLeft,
                    OffsetY = newTop
                });
            }

            base.OnPointerMoved(e);
        }
    }
    public class BorderMovedEventArgs : EventArgs
    {
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }
    }
    public class BorderResizedEventArgs : EventArgs
    {
        public double NewWidth { get; set; }
        public double NewHeight { get; set; }
        public double NewX { get; set; }
        public double NewY { get; set; }
    }
}
