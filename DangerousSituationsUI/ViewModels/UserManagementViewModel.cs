using Avalonia.Collections;
using ClassLibrary.Database.Models;
using ReactiveUI;
using Serilog;
using System.Threading.Tasks;
using System;
using System.Linq;
using DangerousSituationsUI.Services;
using System.Reactive;
using System.Reactive.Linq;

namespace DangerousSituationsUI.ViewModels
{
    public class UserManagementViewModel : ReactiveObject, IRoutableViewModel
    {
        #region Private Fields
        private readonly UserService _userService;
        private readonly DialogService _dialogService;
        private AvaloniaList<UserItemModel> _userItems = new();
        #endregion


        #region ViewModel Settings
        public IScreen HostScreen { get; }
        public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);
        #endregion


        #region Properties
        public AvaloniaList<UserItemModel> UserItems
        {
            get => _userItems;
            set => this.RaiseAndSetIfChanged(ref _userItems, value);
        }

        private UserItemModel _selectedUser;
        public UserItemModel SelectedUser
        {
            get => _selectedUser;
            set => this.RaiseAndSetIfChanged(ref _selectedUser, value);
        }
        #endregion


        #region Commands
        public ReactiveCommand<Unit, Unit> AddUserCommand { get; }
        public ReactiveCommand<Unit, Unit> DeleteUserCommand { get; }
        #endregion

        #region Constructor
        public UserManagementViewModel(IScreen screen, UserService userService, DialogService dialogService)
        {
            HostScreen = screen;
            _userService = userService;
            _dialogService = dialogService;

            AddUserCommand = ReactiveCommand.CreateFromTask(AddUser);
            DeleteUserCommand = ReactiveCommand.CreateFromTask(DeleteUser,
                this.WhenAnyValue(x => x.SelectedUser).Select(user => user != null));

            LoadUsers();
        }
        #endregion


        #region Private Methods
        private async void LoadUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                UserItems = new AvaloniaList<UserItemModel>(
                    users.Select(u => new UserItemModel(this, u)));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка загрузки пользователей");
            }
        }

        private async Task DeleteUser()
        {
            try
            {
                if (SelectedUser is not null)
                {
                    await _userService.DeleteUserAsync(SelectedUser.UserId);
                    LoadUsers();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка удаления пользователя");
            }
        }

        private async Task AddUser()
        {
            try
            {
                var addUserViewModel = new AddUserViewModel();
                var result = await _dialogService
                    .ShowDialogAsync<AddUserViewModel, AddUserDialogResult>(addUserViewModel);

                if (result != null)
                {
                    if (!ValidateNickname(result.Username, out var usernameError))
                    {
                        await ShowMessageBox("Ошибка", usernameError);
                        return;
                    }

                    if (!ValidatePassword(result.Password, out var passwordError))
                    {
                        await ShowMessageBox("Ошибка", passwordError);
                        return;
                    }

                    if (!ValidateEmail(result.Email, out var emailError))
                    {
                        await ShowMessageBox("Ошибка", emailError);
                        return;
                    }

                    await _userService.AddUserAsync(
                        result.Username,
                        result.Email,
                        result.Password,
                        result.IsAdmin
                    );
                    LoadUsers();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка добавления пользователя");
            }
        }

        private async Task ShowMessageBox(string caption, string message)
        {
            var messageBox = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(caption, message);
            await messageBox.ShowAsync();
        }

        #endregion

        #region Public Methods
        public async Task UpdateUserAdminStatus(int userId, bool isAdmin)
        {
            try
            {
                await _userService.UpdateUserAdminStatusAsync(userId, isAdmin);
                Log.Information($"Для пользователя {userId} изменен статус администратора");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка обновления статуса администратора");
                var user = UserItems.FirstOrDefault(u => u.UserId == userId);
                if (user != null)
                    user.UserAdmin = !isAdmin;
            }
        }

        public void UpdateUsersList()
        {
            LoadUsers();
        }


        public static bool ValidateNickname(string nickname, out string error)
        {
            if (string.IsNullOrWhiteSpace(nickname))
            {
                error = "Имя пользователя не может быть пустым.";
                return false;
            }
            if (nickname.Length < 3)
            {
                error = "Имя пользователя должно быть минимум 3 символа.";
                return false;
            }
            error = null;
            return true;
        }

        public static bool ValidatePassword(string password, out string error)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length <= 5)
            {
                error = "Пароль должен содержать минимум 6 символов.";
                return false;
            }
            if (!password.Any(char.IsUpper) || !password.Any(char.IsPunctuation) || !password.Any(char.IsDigit))
            {
                error = "Пароль должен содержать заглавные буквы, цифры и знаки препинания.";
                return false;
            }
            error = null;
            return true;
        }

        public static bool ValidateEmail(string email, out string error)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                error = null;
                return true;
            }
            catch
            {
                error = "Некорректный адрес электронной почты.";
                return false;
            }
        }

        #endregion

        #region Classes
        public class UserItemModel : ReactiveObject
        {
            private readonly User _user;
            private readonly UserManagementViewModel _parentVm;

            public int UserId => _user.Id;
            public string UserName => _user.Name;

            public bool UserAdmin
            {
                get => _user.IsAdmin;
                set
                {
                    if (_user.IsAdmin != value)
                    {
                        _user.IsAdmin = value;
                        this.RaisePropertyChanged();
                        _ = _parentVm.UpdateUserAdminStatus(UserId, value);
                    }
                }
            }

            public UserItemModel(UserManagementViewModel parentVm, User user)
            {
                _parentVm = parentVm;
                _user = user;
            }
        }

        public record AddUserDialogResult(
            string Username,
            string Email,
            string Password,
            bool IsAdmin
        );
        #endregion
    }
}
