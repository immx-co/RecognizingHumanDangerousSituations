using ClassLibrary;
using ClassLibrary.Database;
using ClassLibrary.Database.Models;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive;
using System.Threading;

namespace DangerousSituationsUI.ViewModels;

public class AuthorizationViewModel : ReactiveObject, IRoutableViewModel
{
    IServiceProvider _serviceProvider;

    PasswordHasher _hasher;

    #region View Model Settings
    public IScreen HostScreen { get; }

    public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

    public CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    #endregion

    #region Private Properties
    private string _nickname;

    private string _password;

    private string _activeUsername = "";
    #endregion

    #region Public Properties
    public string Nickname
    {
        get => _nickname;
        set => this.RaiseAndSetIfChanged(ref _nickname, value);
    }

    public string Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    public string ActiveUsername
    {
        get => _activeUsername;
        set => this.RaiseAndSetIfChanged(ref _activeUsername, value);
    }
    #endregion

    public ReactiveCommand<Unit, Unit> LoginCommand { get; }

    public ReactiveCommand<Unit, Unit> BackCommand { get; }

    public AuthorizationViewModel(IScreen screen, IServiceProvider serviceProvider, PasswordHasher hasher)
    {
        HostScreen = screen;

        _serviceProvider = serviceProvider;
        _hasher = hasher;

        LoginCommand = ReactiveCommand.Create(Login);
        BackCommand = ReactiveCommand.Create(() =>
        {
            ActiveUsername = "";
            Nickname = string.Empty;
            Password = string.Empty;
            HostScreen.Router.NavigateBack.Execute().Subscribe();
        });
    }

    #region Private Methods
    private async void Login()
    {
        using ApplicationContext db = _serviceProvider.GetRequiredService<ApplicationContext>();

        User? dbUser = db.Users.SingleOrDefault(user => user.Name == Nickname);
        if (dbUser is null)
        {
            ShowMessageBox("Invalid Username", $"Имени пользователя {Nickname} не существует");
            Nickname = string.Empty;
            Password = string.Empty;
            return;
        }

        bool isUnhashedPassword = _hasher.VerifyPassword(Password, dbUser.HashPassword);
        if (!isUnhashedPassword)
        {
            Password = string.Empty;
            ShowMessageBox("Invalid Password", "Неверный пароль! Попробуйте еще раз.");
            return;
        }

        ActiveUsername = Nickname;
        Nickname = string.Empty;
        Password = string.Empty;

        HostScreen.Router.Navigate.Execute(_serviceProvider.GetRequiredService<MainViewModel>());
        _serviceProvider.GetRequiredService<NavigationViewModel>().IsAppButtonsEnable = true;

        if (dbUser.IsAdmin) 
        {
            _serviceProvider.GetRequiredService<NavigationViewModel>().IsAdminPrivilege = true;
        }
    }

    private void ShowMessageBox(string caption, string message)
    {
        var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandard(caption, message);
        messageBoxStandardWindow.ShowAsync();
    }
    #endregion
}
