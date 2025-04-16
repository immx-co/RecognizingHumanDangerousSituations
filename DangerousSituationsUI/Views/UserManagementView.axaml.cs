using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using DangerousSituationsUI.ViewModels;


namespace DangerousSituationsUI.Views
{
    public partial class UserManagementView : ReactiveUserControl<UserManagementViewModel>
    {
        public UserManagementView()
        {
            this.WhenActivated(disposables => { });
            AvaloniaXamlLoader.Load(this);
        }
    }
}
