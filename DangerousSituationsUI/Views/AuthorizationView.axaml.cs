using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DangerousSituationsUI.ViewModels;
using ReactiveUI;

namespace DangerousSituationsUI.Views;

public partial class AuthorizationView : ReactiveUserControl<AuthorizationViewModel>
{
    public AuthorizationView()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}