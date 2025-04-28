using ReactiveUI;
using System.Reactive;
using static DangerousSituationsUI.ViewModels.UserManagementViewModel;

namespace DangerousSituationsUI.ViewModels;

public class AddUserViewModel : ReactiveObject
{
    private string _username;
    public string Username
    {
        get => _username;
        set => this.RaiseAndSetIfChanged(ref _username, value);
    }

    private string _email;
    public string Email
    {
        get => _email;
        set => this.RaiseAndSetIfChanged(ref _email, value);
    }

    private string _password;
    public string Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    public bool IsAdmin { get; set; }


    public ReactiveCommand<Unit, AddUserDialogResult> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public AddUserViewModel()
    {
        var canSave = this.WhenAnyValue(
            x => x.Username,
            x => x.Email,
            x => x.Password,
            (name, email, pass) =>
                !string.IsNullOrWhiteSpace(name) &&
                !string.IsNullOrWhiteSpace(email) &&
                !string.IsNullOrWhiteSpace(pass)
        );

        SaveCommand = ReactiveCommand.Create(
            () => new AddUserDialogResult(Username, Email, Password, IsAdmin),
            canSave
        );

        CancelCommand = ReactiveCommand.Create(() => { });
    }

}




