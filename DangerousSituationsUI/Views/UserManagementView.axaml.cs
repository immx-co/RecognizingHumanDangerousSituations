using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DangerousSituationsUI.ViewModels;
using ReactiveUI;

namespace DangerousSituationsUI.Views;

public partial class UserManagementView : ReactiveUserControl<UserManagementViewModel>
{
    public UserManagementView()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}