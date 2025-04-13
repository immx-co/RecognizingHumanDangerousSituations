using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Reactive;
using System.Threading;

namespace DangerousSituationsUI.ViewModels;

public class InputApplicationViewModel : ReactiveObject, IRoutableViewModel
{
    #region View Model Settings
    public IScreen HostScreen { get; }

    public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

    public CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    #endregion

    IServiceProvider _serviceProvider;

    public ReactiveCommand<Unit, Unit> GoAuthorization { get; }

    public ReactiveCommand<Unit, Unit> GoRegistration { get; }

    #region Contructor
    public InputApplicationViewModel(IScreen screen, IServiceProvider serviceProvider)
    {
        HostScreen = screen;

        _serviceProvider = serviceProvider;

        GoAuthorization = ReactiveCommand.Create(GoToAuthorizationView);
        GoRegistration = ReactiveCommand.Create(GoToRegistrationView);
    }
    #endregion

    #region Private Methods
    private async void GoToAuthorizationView()
    {
        HostScreen.Router.Navigate.Execute(_serviceProvider.GetRequiredService<AuthorizationViewModel>());
    }

    private async void GoToRegistrationView()
    {
        HostScreen.Router.Navigate.Execute(_serviceProvider.GetRequiredService<RegistrationViewModel>());
    }
    #endregion
}
