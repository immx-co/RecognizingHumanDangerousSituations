using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.LogicalTree;
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
        if (DataContext is VideoEventJournalViewModel viewModel && sender is InteractiveBorder border)
        {
            border.ImageHeight = 400;
            border.ImageWidth = (int)((400 * viewModel.CurrentImage.Size.Width) / viewModel.CurrentImage.Size.Height);
            border.ImageOffset = (800 - border.ImageWidth) / 2;

            viewModel.BoxTopLeftX = (int)e.OffsetX;
            viewModel.BoxTopLeftY = (int)e.OffsetY;
            viewModel.BoxPositionChanged = true;


            if (border.DataContext is RectItem rect)
            {
                rect.X = (int)e.OffsetX;
                rect.Y = (int)e.OffsetY;
            }
        }
    }
    private void InteractiveBorder_BorderResized(object? sender, BorderResizedEventArgs e)
    {
        if (DataContext is VideoEventJournalViewModel viewModel && sender is InteractiveBorder border)
        {
            viewModel.BoxWidth = (int)e.NewWidth;
            viewModel.BoxHeight = (int)e.NewHeight;
            viewModel.BoxTopLeftX = (int)e.NewX;
            viewModel.BoxTopLeftY = (int)e.NewY;


            border.ImageHeight = 400;
            border.ImageWidth = (int)((400 * viewModel.CurrentImage.Size.Width) / viewModel.CurrentImage.Size.Height);
            border.ImageOffset = (800 - border.ImageWidth) / 2;

            viewModel.BoxPositionChanged = true;

            if (border.DataContext is RectItem rect)
            {
                rect.Width = (int)e.NewWidth;
                rect.Height = (int)e.NewHeight;
                rect.X = (int)e.NewX;
                rect.Y = (int)e.NewY;
            }
        }
    }

    private void ResizeHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Rectangle resizeHandle && DataContext is VideoEventJournalViewModel viewModel)
        {
            viewModel.BoxPositionChanged = true;
            var border = resizeHandle.Parent.GetLogicalParent() as InteractiveBorder;
            
            border.ImageHeight = 400;
            border.ImageWidth = (int)((400 * viewModel.CurrentImage.Size.Width) / viewModel.CurrentImage.Size.Height);
            border.ImageOffset = (800 - border.ImageWidth) / 2;
        }
    }

    private void DataGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.PropertyName == "Name")
            e.Cancel = true;
    }
}