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
        public Interaction<Unit, Unit> ShowAddUserDialogInteraction { get; } = new();

        #region Private Fields
        private readonly UserService _userService;
        private AvaloniaList<UserItemModel> _userItems = new();
        #endregion


        #region View Model Settings
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
        public UserManagementViewModel(IScreen screen, UserService userService)
        {
            HostScreen = screen;
            _userService = userService;

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

        #endregion


        #region Methods
        public async Task UpdateUserAdminStatus(int userId, bool isAdmin)
        {
            try
            {
                await _userService.UpdateUserAdminStatusAsync(userId, isAdmin);
                Log.Information($"Для {userId} изменен статус администратора");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка обновления статуса администратора");
                var user = UserItems.FirstOrDefault(u => u.UserId == userId);
                if (user != null)
                    user.UserAdmin = !isAdmin;
            }
        }

        private async Task AddUser()
        {
            try
            {
                await ShowAddUserDialogInteraction.Handle(Unit.Default);
                LoadUsers();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка добавления пользователя");
            }
        }


        public async Task AddUserFromDialogAsync(AddUserWindow dialog)
        {
            try
            {
                await _userService.AddUserAsync(
                    dialog.Username,
                    dialog.Email,
                    dialog.Password,
                    dialog.IsAdmin
                );

                LoadUsers();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка при добавлении пользователя из диалога");
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

            public UserItemModel(UserManagementViewModel parentVm, User user)
            {
                _parentVm = parentVm;
                _user = user;
            }
        }
        #endregion

    }

}


