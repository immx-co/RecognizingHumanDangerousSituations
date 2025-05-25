using Avalonia.Controls;
using DangerousSituationsUI.ViewModels;
using DangerousSituationsUI.Views;
using System;
using System.Threading.Tasks;

namespace DangerousSituationsUI.Services
{
    public class DialogService
    {
        public async Task<T?> ShowDialogAsync<TViewModel, T>(TViewModel viewModel)
            where TViewModel : class
            where T : class
        {
            if (App.Current?.CurrentWindow is not Window mainWindow)
                throw new InvalidOperationException("Нет главного окна");

            Window dialog;

            if (viewModel is AddUserViewModel addUserViewModel)
            {
                var addUserWindow = new AddUserWindow
                {
                    DataContext = addUserViewModel,
                    ViewModel = addUserViewModel
                };

                addUserViewModel.CancelCommand.Subscribe(_ =>
                {
                    addUserWindow.Close(null);
                });

                dialog = addUserWindow;
            }
            else
            {
                throw new InvalidOperationException("View model не поддерживает диалог");
            }

            return await dialog.ShowDialog<T>(mainWindow);
        }
    }
}
