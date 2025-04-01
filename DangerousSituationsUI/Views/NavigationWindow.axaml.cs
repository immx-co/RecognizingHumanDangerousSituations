using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DangerousSituationsUI.ViewModels;
using ReactiveUI;

namespace DangerousSituationsUI.Views;

public partial class NavigationWindow : ReactiveWindow<NavigationViewModel>
{
    public NavigationWindow()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }

    private void WindowPointerMoved(object sender, PointerPressedEventArgs e)
    {
        var position = e.GetPosition(this);
        double topAreaHeight = 35;

        if (position.Y <= topAreaHeight && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}