using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DangerousSituationsUI.ViewModels;
using ReactiveUI;
using System.Reactive;
using Avalonia.VisualTree;

namespace DangerousSituationsUI.Views
{
    public partial class UserManagementView : ReactiveUserControl<UserManagementViewModel>
    {
        public UserManagementView()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                ViewModel?.ShowAddUserDialogInteraction.RegisterHandler(async interaction =>
                {
                    var owner = this.GetVisualRoot() as Window;
                    if (owner is not null)
                    {
                        var dialog = new AddUserWindow();
                        var result = await dialog.ShowDialog<bool>(owner);

                        if (result)
                        {
                            await ViewModel.AddUserFromDialogAsync(dialog);
                        }
                    }

                    interaction.SetOutput(Unit.Default);
                });
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

