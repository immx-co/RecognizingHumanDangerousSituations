using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DangerousSituationsUI.Services;
using DangerousSituationsUI.ViewModels;

namespace DangerousSituationsUI.Views;

public partial class AddControlZoneDialogWindow : Window
{
    public AddControlZoneDialogWindow()
    {
        InitializeComponent();
    }

    private void Canvas_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs args)
    {
        if(DataContext is AddControlZoneDialogViewModel viewModel)
        {
            var point = args.GetCurrentPoint(sender as Control);
            var x = point.Position.X;
            var y = point.Position.Y;

            viewModel.Points.Add(new ZonePoint
            {
                X = x,
                Y = y
            });
        }
    }
}