using Avalonia.Collections;
using ClassLibrary.Database.Models;
using ReactiveUI;
using Serilog;
using System.Threading.Tasks;
using System;
using System.Linq;
using DangerousSituationsUI.Services;


namespace DangerousSituationsUI.ViewModels
{

    public class UserManagementViewModel : ReactiveObject, IRoutableViewModel
    {

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
        #endregion


        #region Constructor
        public UserManagementViewModel(IScreen screen, UserService userService)
        {
            HostScreen = screen;
            _userService = userService;
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


