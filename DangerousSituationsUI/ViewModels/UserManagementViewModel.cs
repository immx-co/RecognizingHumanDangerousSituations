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
                Log.Error($"Ошибка загрузки пользователей: {ex}");
            }
        }

        private async Task DeleteUser()
        {
            try
            {
                if (SelectedUser is not null)
                {
                    var name = SelectedUser.UserName;
                    await _userService.DeleteUserAsync(SelectedUser.UserId);
                    Log.Information($"Удален пользователь {name}");
                    LoadUsers();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка удаления пользователя: {ex}");
            }
        }

        private async Task AddUser()
        {
            try
            {
                var addUserViewModel = new AddUserViewModel();
                var result = _dialogService
                    .ShowDialogAsync<AddUserViewModel, AddUserDialogResult>(addUserViewModel);

                addUserViewModel.SaveCommand.Subscribe(async result =>
                {
                    var (isValid, errorMessage) = await _userService
                        .ValidateNewUserAsync(result.Username, result.Email, result.Password);
                    
                    if (!isValid)
                    {
                        await ShowMessageBox("Ошибка", errorMessage);
                        return;
                    }

                    await _userService.AddUserAsync(
                        result.Username,
                        result.Email,
                        result.Password,
                        result.IsAdmin
                    );

                    Log.Information($"Создан пользователь {result.Username}.");
                    LoadUsers();
                });
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка добавления пользователя: {ex}");
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
                Log.Information($"Для пользователя {userId} изменен статус администратора.");
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка обновления статуса администратора: {ex}");
                var user = UserItems.FirstOrDefault(u => u.UserId == userId);
                if (user != null)
                    user.UserAdmin = !isAdmin;
            }
        }
        public async Task UpdateUserDeletedStatus(int userId, bool isDeleted)
        {
            try
            {
                await _userService.UpdateUserDeletedStatusAsync(userId, isDeleted);
                Log.Information($"Для пользователя {userId} изменен статус удаления.");
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка обновления статуса удаления: {ex}");
                var user = UserItems.FirstOrDefault(u => u.UserId == userId);
                if (user != null)
                    user.IsDeleted = !isDeleted;
            }
        }
        public void UpdateUsersList()
        {
            LoadUsers();
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
            public bool IsDeleted
            {
                get => _user.IsDeleted;
                set
                {
                    if (_user.IsDeleted != value)
                    {
                        _user.IsDeleted = value;
                        this.RaisePropertyChanged();
                        _ = _parentVm.UpdateUserDeletedStatus(UserId, value);
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
