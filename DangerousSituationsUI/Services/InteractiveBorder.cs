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
        private Point _topRight;
        private Point _bottomLeft;
        private Point _bottomRight;
        private Point _initialSize; // Начальный размер границы
        private Point _positionInBlock; // Позиция указателя внутри границы
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
        public Point TopRight
        {
            get => _topRight;
            set => _topRight = value;
        }
        public Point BottomLeft
        {
            get => _bottomLeft;
            set => _bottomLeft = value;
        }
        public Point BottomRight
        {
            get => _bottomRight;
            set => _bottomRight = value;
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

            BottomLeft = new Point(TopLeft.X, TopLeft.Y + this.Height);
            BottomRight = new Point(TopLeft.X + this.Width, TopLeft.Y + this.Height);
            TopRight = new Point(TopLeft.X + this.Width, TopLeft.Y + this.Height);

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
                    ImageOffset = _image_offset
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
                var currentPointerPosition = e.GetPosition(this);

                var deltaX = currentPointerPosition.X - _positionInBlock.X;
                var deltaY = currentPointerPosition.Y - _positionInBlock.Y;

                double newWidth = Math.Max(20, _initialSize.X + deltaX);
                double newHeight = Math.Max(20, _initialSize.Y + deltaY);

                Width = newWidth;
                Height = newHeight;

                BorderResized?.Invoke(this, new BorderResizedEventArgs
                {
                    NewWidth = newWidth,
                    NewHeight = newHeight
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
        public Point GetCurrentOffset()
        {
            return _transform != null ? new Point(TopLeft.X + _transform.X, TopLeft.Y + _transform.Y) : TopLeft;
        }
        public void ResetTransform()
        {
            _transform = new TranslateTransform(0, 0);
            RenderTransform = _transform;
        }
    }
    public class BorderMovedEventArgs : EventArgs
    {
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }
        public double ImageOffset { get; set; }
    }
    public class BorderResizedEventArgs : EventArgs
    {
        public double NewWidth { get; set; }
        public double NewHeight { get; set; }
    }
}
