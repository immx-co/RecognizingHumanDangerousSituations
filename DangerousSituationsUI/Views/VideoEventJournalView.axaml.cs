using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ClassLibrary.Services;
using DangerousSituationsUI.Services;
using DangerousSituationsUI.ViewModels;
using ReactiveUI;
using System.Linq;

namespace DangerousSituationsUI.Views;

public partial class VideoEventJournalView : ReactiveUserControl<VideoEventJournalViewModel>
{
    public VideoEventJournalView()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
    
    private void InteractiveBorder_BorderMoved(object? sender, BorderMovedEventArgs e)
    {
        if (DataContext is VideoEventJournalViewModel viewModel)
        {
            viewModel.BoxX = (int)e.OffsetX;
            viewModel.BoxY = (int)e.OffsetY;
            viewModel.BoxPositionChanged = true;
            
            if (sender is InteractiveBorder border && border.DataContext is RectItem rect)
            {
                rect.X = (int)e.OffsetX;
                rect.Y = (int)e.OffsetY;
            }
        }
    }
    private void InteractiveBorder_BorderResized(object? sender, BorderResizedEventArgs e)
    {
        if (DataContext is VideoEventJournalViewModel viewModel)
        {
            viewModel.BoxWidth = (int)e.NewWidth;
            viewModel.BoxHeight = (int)e.NewHeight;
            viewModel.BoxPositionChanged = true;
            
            if (sender is InteractiveBorder border && border.DataContext is RectItem rect)
            {
                rect.Width = (int)e.NewWidth;
                rect.Height = (int)e.NewHeight;
            }
        }
    }

    private void ResizeHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Rectangle resizeHandle && DataContext is VideoEventJournalViewModel viewModel)
        {
            viewModel.BoxPositionChanged = true;
        }
    }
}