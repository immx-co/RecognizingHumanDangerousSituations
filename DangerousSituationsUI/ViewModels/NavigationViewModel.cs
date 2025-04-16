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

    #region Private Properties
    private bool _isAppButtonsEnable = false;
    private bool _isAdminPrivilege = false;

    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    #endregion

    #region Public Properties
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
    #endregion

    #region Public Commands
    public ReactiveCommand<Unit, Unit> GoMainWindow { get; }

    public ReactiveCommand<Unit, Unit> GoEventJournalWindow { get; }

    public ReactiveCommand<Unit, Unit> GoVideoEventJournalWindow { get; }

    public ReactiveCommand<Unit, Unit> GoConfiguration { get; }

    public ReactiveCommand<Unit, Unit> GoInputApplicationWindow { get; }

    public ReactiveCommand<Unit, Unit> GoUserManagement { get; }

    #endregion

    public NavigationViewModel(IScreen screenRealization, IServiceProvider serviceProvider)
    {
        Router = screenRealization.Router;
        _serviceProvider = serviceProvider;

        GoMainWindow = ReactiveCommand.Create(NavigateToMainWindow);
        GoConfiguration = ReactiveCommand.Create(NavigateToConfigurationWindow);
        GoVideoEventJournalWindow = ReactiveCommand.Create(NavigateToVideoEventJournalWindow);
        GoInputApplicationWindow = ReactiveCommand.Create(NavigateToInputApplicationWindow);
        GoUserManagement = ReactiveCommand.Create(NavigateToUserManagementWindow);

        Router.CurrentViewModel.Subscribe(currentVm =>
        {
            if (currentVm is InputApplicationViewModel)
            {
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
    }

    private void NavigateToUserManagementWindow()
    {
        CheckDisposedCancelletionToken();
        Router.Navigate.Execute(_serviceProvider.GetRequiredService<UserManagementViewModel>());
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
    public void Dispose()
    {
        _disposables?.Dispose();
    }
    #endregion
}
