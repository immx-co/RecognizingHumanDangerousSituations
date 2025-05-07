using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;
using MouseButton = Avalonia.Remote.Protocol.Input.MouseButton;

namespace DangerousSituationsUI.Services
{
    public class InteractiveBorder : Border
    {
        private bool _isPressed;
        private Point _positionInBlock;
        private TranslateTransform _transform = null!;
        public event EventHandler<BorderMovedEventArgs> BorderMoved;


        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            _isPressed = true;
            _positionInBlock = e.GetPosition((Visual?)Parent);

            if (_transform != null!)
                _positionInBlock = new Point(
                    _positionInBlock.X - _transform.X,
                    _positionInBlock.Y - _transform.Y
                );

            base.OnPointerPressed(e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            _isPressed = false;

            if (_transform != null && BorderMoved != null)
            {
                BorderMoved(this, new BorderMovedEventArgs
                {
                    OffsetX = _transform.X,
                    OffsetY = _transform.Y
                });
            }

            base.OnPointerReleased(e);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (!_isPressed)
                return;

            if (Parent == null)
                return;

            var currentPosition = e.GetPosition((Visual?)Parent);

            var offsetX = currentPosition.X - _positionInBlock.X;
            var offsetY = currentPosition.Y - _positionInBlock.Y;

            _transform = new TranslateTransform(offsetX, offsetY);
            RenderTransform = _transform;

            BorderMoved?.Invoke(this, new BorderMovedEventArgs
            {
                OffsetX = offsetX,
                OffsetY = offsetY
            });

            base.OnPointerMoved(e);
        }

        public Point GetCurrentOffset()
        {
            return _transform != null ? new Point(_transform.X, _transform.Y) : new Point(0, 0);
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
}