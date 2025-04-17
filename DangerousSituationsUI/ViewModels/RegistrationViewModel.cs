using ClassLibrary;
using ClassLibrary.Database;
using ClassLibrary.Database.Models;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Reactive;
using System.Threading;

namespace DangerousSituationsUI.ViewModels;

public class RegistrationViewModel : ReactiveObject, IRoutableViewModel
{
    #region View Model Settings
    public IScreen HostScreen { get; }

    public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

    public CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    #endregion

    #region Private Properties
    private string _nickname;

    private string _password;

    private string _email;
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

    public string Email
    {
        get => _email;
        set => this.RaiseAndSetIfChanged(ref _email, value);
    }
    #endregion

    IServiceProvider _serviceProvider;

    PasswordHasher _hasher;

    public ReactiveCommand<Unit, Unit> RegistrationCommand { get; }

    public ReactiveCommand<Unit, Unit> BackCommand { get; }

    public RegistrationViewModel(IScreen screen, IServiceProvider serviceProvider, PasswordHasher hasher)
    {
        HostScreen = screen;

        _serviceProvider = serviceProvider;
        _hasher = hasher;

        RegistrationCommand = ReactiveCommand.Create(Registration);

        BackCommand = ReactiveCommand.Create(() =>
        {
            Nickname = string.Empty;
            Password = string.Empty;
            Email = string.Empty;
            HostScreen.Router.NavigateBack.Execute().Subscribe();
        });
    }

    #region Private Methods
    private async void Registration()
    {
        using ApplicationContext db = _serviceProvider.GetRequiredService<ApplicationContext>();

        List<User> dbUsers = db.Users.ToList();
        if (dbUsers.Any(user => user.Name == Nickname))
        {
            ShowMessageBox("Failed", $"Пользователь с именем {Nickname} уже существует. Попробуйте другое выбрать другое имя пользователя.");
            Nickname = string.Empty;
            return;
        }

        if (Password.Length <= 5)
        {
            ShowMessageBox("Invalid Password", "Допустимая длина пароля — от 5 символов.");
            Password = string.Empty;
            return;
        }

        if (!Password.Any(ch => char.IsUpper(ch)) || !Password.Any(ch => char.IsPunctuation(ch)) || !Password.Any(ch => char.IsDigit(ch)))
        {
            ShowMessageBox("Invalid Password", "Пароль должен содержать латинские буквы в верхнем регистре, цифры и знаки препинания.");
            Password = string.Empty;
            return;
        }

        if (!IsValidEmail(Email))
        {
            ShowMessageBox("Invalid Email", $"Почта {Email} невалидна. Попробуйте указать другую почту.");
            Email = string.Empty;
            return;
        }

        string hashedPassword = _hasher.HashPassword(Password);
        User registeredUser = new User { Name = Nickname, HashPassword = hashedPassword, Email = Email, IsAdmin = false };

        db.Users.AddRange(registeredUser);
        db.SaveChanges();

        ShowMessageBox("Success", $"Регистрация пользователя {Nickname} прошла успешно!");

        Nickname = string.Empty;
        Password = string.Empty;
        Email = string.Empty;

        HostScreen.Router.Navigate.Execute(_serviceProvider.GetRequiredService<AuthorizationViewModel>());
        return;
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var mailAddress = new MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private void ShowMessageBox(string caption, string message)
    {
        var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandard(caption, message);
        messageBoxStandardWindow.ShowAsync();
    }
    #endregion
}
