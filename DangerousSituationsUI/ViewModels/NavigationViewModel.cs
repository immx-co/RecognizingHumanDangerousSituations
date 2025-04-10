using System.Reactive.Linq;
using System.Linq;
using ReactiveUI;
using System.Reactive;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace DangerousSituationsUI.ViewModels;

public class NavigationViewModel : ReactiveObject
{
    private readonly IServiceProvider _serviceProvider;
    public RoutingState Router { get; }

    public ReactiveCommand<Unit, Unit> GoMainWindow { get; }

    public ReactiveCommand<Unit, Unit> GoEventJournalWindow { get; }

    public ReactiveCommand<Unit, Unit> GoVideoEventJournalWindow { get; }

    public ReactiveCommand<Unit, Unit> GoConfiguration { get; }

    public ReactiveCommand<Unit, Unit> GoInputApplicationWindow { get; }

    public NavigationViewModel(IScreen screenRealization, IServiceProvider serviceProvider)
    {
        Router = screenRealization.Router;
        _serviceProvider = serviceProvider;

        GoMainWindow = ReactiveCommand.Create(NavigateToMainWindow);
        GoConfiguration = ReactiveCommand.Create(NavigateToConfigurationWindow);
        GoVideoEventJournalWindow = ReactiveCommand.Create(NavigateToVideoEventJournalWindow);
        GoInputApplicationWindow = ReactiveCommand.Create(NavigateToInputApplicationWindow);

        Router.Navigate.Execute(_serviceProvider.GetRequiredService<InputApplicationViewModel>());
    }

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
}
