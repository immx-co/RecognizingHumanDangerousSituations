using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ClassLibrary.Services;
using DangerousSituationsUI.Services;
using DangerousSituationsUI.ViewModels;
using ReactiveUI;

namespace DangerousSituationsUI.Views;

public partial class VideoEventJournalView : ReactiveUserControl<VideoEventJournalViewModel>
{

    public VideoEventJournalView()
    {
        AvaloniaXamlLoader.Load(this);
        this.WhenActivated(disposables => { });
    }
    
    private void InteractiveBorder_BorderMoved(object? sender, BorderMovedEventArgs e)
    {
        if (DataContext is VideoEventJournalViewModel viewModel)
        {
            viewModel.BoxTopLeftX = (int)e.OffsetX;
            viewModel.BoxTopLeftY = (int)e.OffsetY;
            
            viewModel.BoxPositionChanged = true;


            if (sender is InteractiveBorder border && border.DataContext is RectItem rect)
            {
                rect.X = (int)e.OffsetX;
                rect.Y = (int)e.OffsetY;
                
                border.ImageHeight = viewModel.ImageHeight;
                border.ImageWidth = viewModel.ImageWidth;
                border.ImageOffset = (800 - viewModel.ImageWidth) / 2;
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

                border.ImageHeight = viewModel.ImageHeight;
                border.ImageWidth = viewModel.ImageWidth;
                border.ImageOffset = (viewModel.ImageHeight - viewModel.ImageWidth) / 2;
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

    private void DataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid dataGrid && DataContext is VideoEventJournalViewModel viewModel)
        {
            viewModel.ImageHeight = 400;
            viewModel.ImageWidth = (int)((400 * viewModel.CurrentImage.Size.Width) / viewModel.CurrentImage.Size.Height);
        }
    }
}