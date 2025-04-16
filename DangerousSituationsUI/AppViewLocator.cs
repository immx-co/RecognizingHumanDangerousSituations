using DangerousSituationsUI.ViewModels;
using DangerousSituationsUI.Views;
using ReactiveUI;
using System;

namespace DangerousSituationsUI;

public class AppViewLocator : IViewLocator
{
    public IViewFor ResolveView<T>(T viewModel, string contract = null) => viewModel switch
    {
        InputApplicationViewModel context => new InputApplicationView { ViewModel = context },
        MainViewModel context => new MainView { ViewModel = context },
        VideoEventJournalViewModel context => new VideoEventJournalView { ViewModel = context },
        ConfigurationViewModel context => new ConfigurationView { ViewModel = context },
        RegistrationViewModel context => new RegistrationView { ViewModel = context },
        AuthorizationViewModel context => new AuthorizationView { ViewModel = context },
        UserManagementViewModel context => new UserManagementView { ViewModel = context },
        LogJournalViewModel context => new LogJournalView { ViewModel = context },
        _ => throw new ArgumentOutOfRangeException(nameof(viewModel))
    };
}
