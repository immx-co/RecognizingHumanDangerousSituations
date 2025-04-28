using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DangerousSituationsUI.ViewModels;
using ReactiveUI;

namespace DangerousSituationsUI;

public partial class AddUserWindow : ReactiveWindow<AddUserViewModel>
{
    public AddUserWindow()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}
