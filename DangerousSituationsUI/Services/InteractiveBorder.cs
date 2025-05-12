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
        private Point _initialPosition; // Начальная позиция относительно контейнера
        private Point _initialSize; // Начальный размер границы
        private Point _positionInBlock; // Позиция указателя внутри границы
        private TranslateTransform _transform = null!;
        
        public event EventHandler<BorderMovedEventArgs> BorderMoved;
        public event EventHandler<BorderResizedEventArgs> BorderResized;
        public bool IsPressed
        {
            get => _isPressed;
            set => _isPressed = value;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            IsPressed = true;

            // Сохраняем начальную позицию границы относительно контейнера
            _initialPosition = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
            _positionInBlock = e.GetPosition(this);

            if (e.Source is Rectangle rect) // Если нажали на "ручку"
            {
                _isResizing = true;
                _initialSize = new Point(Width, Height);
            }
            else // Если нажали на саму границу
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
                    OffsetX = _initialPosition.X + _transform.X,
                    OffsetY = _initialPosition.Y + _transform.Y
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

                double newWidth = Math.Max(10, _initialSize.X + Math.Abs(deltaX));
                double newHeight = Math.Max(10, _initialSize.Y + Math.Abs(deltaY));

                Width = newWidth;
                Height = newHeight;

                BorderResized?.Invoke(this, new BorderResizedEventArgs
                {
                    NewWidth = newWidth,
                    NewHeight = newHeight
                });
            }
            else if (_isDragging)
            {
                var currentPointerPosition = e.GetPosition((Visual?)Parent);

                var deltaX = currentPointerPosition.X - _initialPosition.X;
                var deltaY = currentPointerPosition.Y - _initialPosition.Y;

                _transform = new TranslateTransform(deltaX, deltaY);
                RenderTransform = _transform;

                BorderMoved?.Invoke(this, new BorderMovedEventArgs
                {
                    OffsetX = _initialPosition.X + deltaX,
                    OffsetY = _initialPosition.Y + deltaY
                });
            }
            base.OnPointerMoved(e);
        }

        public Point GetCurrentOffset()
        {
            return _transform != null ? new Point(_initialPosition.X + _transform.X, _initialPosition.Y + _transform.Y) : _initialPosition;
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
    }

    public class BorderResizedEventArgs : EventArgs
    {
        public double NewWidth { get; set; }
        public double NewHeight { get; set; }
    }
}
