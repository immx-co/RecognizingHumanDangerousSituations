using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DangerousSituationsUI.ViewModels;
using ReactiveUI;

namespace DangerousSituationsUI.Views;

public partial class RegistrationView : ReactiveUserControl<RegistrationViewModel>
{
    public RegistrationView()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}