using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace DangerousSituationsUI.ViewModels;

public class NavigationViewModel : ReactiveObject, IDisposable
{
    private readonly IServiceProvider _serviceProvider;

    public RoutingState Router { get; }

    #region Private Fields
    private bool _isAppButtonsEnable = false;

    private bool _isAdminPrivilege = false;

    private string _currentUser = "User";

    private ISolidColorBrush _connectionStatus;

    private ISolidColorBrush _tgBotConnectionStatus;

    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    #endregion

    #region Properties
    public bool IsAppButtonsEnable
    {
        get => _isAppButtonsEnable;
        set => this.RaiseAndSetIfChanged(ref _isAppButtonsEnable, value);
    }
    public bool IsAdminPrivilege
    {
        get => _isAdminPrivilege;
        set => this.RaiseAndSetIfChanged(ref _isAdminPrivilege, value);
    }

    public string CurrentUser
    {
        get => _currentUser;
        set => this.RaiseAndSetIfChanged(ref _currentUser, value);
    }

    public ISolidColorBrush ConnectionStatus
    {
        get => _connectionStatus;
        set => this.RaiseAndSetIfChanged(ref _connectionStatus, value);
    }

    public ISolidColorBrush TgBotConnectionStatus
    {
        get => _tgBotConnectionStatus;
        set => this.RaiseAndSetIfChanged(ref _tgBotConnectionStatus, value);
    }
    #endregion

    #region Public Commands
    public ReactiveCommand<Unit, Unit> GoMainWindow { get; }

    public ReactiveCommand<Unit, Unit> GoEventJournalWindow { get; }

    public ReactiveCommand<Unit, Unit> GoVideoEventJournalWindow { get; }

    public ReactiveCommand<Unit, Unit> GoConfiguration { get; }

    public ReactiveCommand<Unit, Unit> GoLogJournalWindow { get; }

    public ReactiveCommand<Unit, Unit> GoInputApplicationWindow { get; }

    public ReactiveCommand<Unit, Unit> GoUserManagement { get; }

    public ReactiveCommand<Unit, Unit> GoVideoPlayerWindow { get; }
    #endregion

    public NavigationViewModel(IScreen screenRealization, IServiceProvider serviceProvider)
    {
        Router = screenRealization.Router;
        _serviceProvider = serviceProvider;

        ConnectionStatus = Brushes.Gray;
        TgBotConnectionStatus = Brushes.Gray;

        GoMainWindow = ReactiveCommand.Create(NavigateToMainWindow);
        GoConfiguration = ReactiveCommand.Create(NavigateToConfigurationWindow);
        GoVideoEventJournalWindow = ReactiveCommand.Create(NavigateToVideoEventJournalWindow);
        GoInputApplicationWindow = ReactiveCommand.Create(NavigateToInputApplicationWindow);
        GoVideoPlayerWindow = ReactiveCommand.Create(NavigateToVideoPlayerWindow);
        GoUserManagement = ReactiveCommand.Create(NavigateToUserManagementWindow);
        GoLogJournalWindow = ReactiveCommand.Create(NavigateToLogJournalWindow);

        Router.CurrentViewModel.Subscribe(currentVm =>
        {
            if (currentVm is InputApplicationViewModel)
            {
                ConnectionStatus = Brushes.Gray;
                TgBotConnectionStatus = Brushes.Gray;
                IsAppButtonsEnable = false;
                IsAdminPrivilege = false;
            }
        }).DisposeWith(_disposables);

        Router.Navigate.Execute(_serviceProvider.GetRequiredService<InputApplicationViewModel>());
    }

    #region Private Methods
    private void NavigateToMainWindow()
    {
        CheckDisposedCancelletionToken();
        Router.Navigate.Execute(_serviceProvider.GetRequiredService<MainViewModel>());
    }

    private void NavigateToVideoEventJournalWindow()
    {
        CheckDisposedCancelletionToken();
        Router.Navigate.Execute(_serviceProvider.GetRequiredService<VideoEventJournalViewModel>());
    }

    private void NavigateToConfigurationWindow()
    {
        CheckDisposedCancelletionToken();
        Router.Navigate.Execute(_serviceProvider.GetRequiredService<ConfigurationViewModel>());
    }

    private void NavigateToInputApplicationWindow()
    {
        CheckDisposedCancelletionToken();
        Router.Navigate.Execute(_serviceProvider.GetRequiredService<InputApplicationViewModel>());

        _serviceProvider.GetRequiredService<MainViewModel>().ClearUI();
        _serviceProvider.GetRequiredService<VideoPlayerViewModel>().ClearUI();
        _serviceProvider.GetRequiredService<LogJournalViewModel>().ClearUI();
        _serviceProvider.GetRequiredService<VideoEventJournalViewModel>().ClearUI();
    }
    private void NavigateToLogJournalWindow()
    {
        CheckDisposedCancelletionToken();
        Router.Navigate.Execute(_serviceProvider.GetRequiredService<LogJournalViewModel>());
    }

    private void NavigateToUserManagementWindow()
    {
        CheckDisposedCancelletionToken();
        Router.Navigate.Execute(_serviceProvider.GetRequiredService<UserManagementViewModel>());
    }

    private void NavigateToVideoPlayerWindow()
    {
        CheckDisposedCancelletionToken();
        Router.Navigate.Execute(_serviceProvider.GetRequiredService<VideoPlayerViewModel>());
    }

    private void CheckDisposedCancelletionToken()
    {
        if (Router.NavigationStack.Count > 0)
        {
            var currentViewModel = Router.NavigationStack.Last();
            if (currentViewModel is IDisposable disposableViewModel)
            {
                disposableViewModel.Dispose();
            }
        }
    }
    #endregion

    #region Public Methods
    public string GetParsedUserName()
    {
        if (string.IsNullOrEmpty(CurrentUser))
            return string.Empty;

        string[] parts = CurrentUser.Split(':');
        return parts.Length > 1
            ? parts[1].Trim()
            : string.Empty;
    }

    public void Dispose()
    {
        _disposables?.Dispose();
    }
    #endregion
}
