using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using ClassLibrary.Services;
using DangerousSituationsUI.Services;
using DangerousSituationsUI.ViewModels;

namespace DangerousSituationsUI.Views;

public partial class ControlZoneSettingsWindow : Window
{
    public ControlZoneSettingsWindow()
    {
        InitializeComponent();
    }

    private void InteractiveBorder_BorderMoved(object? sender, BorderMovedEventArgs e)
    {
        if (DataContext is ControlZoneSettingsViewModel viewModel)
        {
            viewModel.BoxTopLeftX = (int)e.OffsetX;
            viewModel.BoxTopLeftY = (int)e.OffsetY;

            viewModel.BoxPositionChanged = true;
            var test = ((InteractiveBorder)sender).DataContext;
            if (sender is InteractiveBorder border && border.DataContext is RectItem rect)
            {
                rect.X = (int)e.OffsetX;
                rect.Y = (int)e.OffsetY;

                border.ImageHeight = 400;
                border.ImageWidth = (int)((400 * viewModel.Frame.Size.Width) / viewModel.Frame.Size.Height);
                border.ImageOffset = (800 - border.ImageWidth) / 2;
            }
        }
    }
    private void InteractiveBorder_BorderResized(object? sender, BorderResizedEventArgs e)
    {
        if (DataContext is ControlZoneSettingsViewModel viewModel)
        {
            viewModel.BoxWidth = (int)e.NewWidth;
            viewModel.BoxHeight = (int)e.NewHeight;
            viewModel.BoxTopLeftX = (int)e.NewX;
            viewModel.BoxTopLeftY = (int)e.NewY;

            viewModel.BoxPositionChanged = true;

            if (sender is InteractiveBorder border && border.DataContext is RectItem rect)
            {
                rect.Width = (int)e.NewWidth;
                rect.Height = (int)e.NewHeight;
                rect.X = (int)e.NewX;
                rect.Y = (int)e.NewY;

                border.ImageHeight = 400;
                border.ImageWidth = (int)((400 * viewModel.Frame.Size.Width) / viewModel.Frame.Size.Height);
                border.ImageOffset = (800 - border.ImageWidth) / 2;
            }
        }
    }

    private void ResizeHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Rectangle resizeHandle && DataContext is ControlZoneSettingsViewModel viewModel)
        {
            viewModel.BoxPositionChanged = true;
            var border = resizeHandle.Parent.GetLogicalParent() as InteractiveBorder;

            border.ImageHeight = 400;
            border.ImageWidth = (int)((400 * viewModel.Frame.Size.Width) / viewModel.Frame.Size.Height);
            border.ImageOffset = (800 - border.ImageWidth) / 2;
        }
    }
}