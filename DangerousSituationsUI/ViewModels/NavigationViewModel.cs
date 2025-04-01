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

    public NavigationViewModel(IScreen screenRealization, IServiceProvider serviceProvider)
    {
        Router = screenRealization.Router;
        _serviceProvider = serviceProvider;

        GoMainWindow = ReactiveCommand.Create(NavigateToMainWindow);
        GoConfiguration = ReactiveCommand.Create(NavigateToConfigurationWindow);
        GoEventJournalWindow = ReactiveCommand.Create(NavigateToEventJournalWindow);
        GoVideoEventJournalWindow = ReactiveCommand.Create(NavigateToVideoEventJournalWindow);

        Router.Navigate.Execute(_serviceProvider.GetRequiredService<MainViewModel>());
    }

    private void NavigateToMainWindow()
    {
        CheckDisposedCancelletionToken();
        Router.Navigate.Execute(_serviceProvider.GetRequiredService<MainViewModel>());
    }

    private void NavigateToEventJournalWindow()
    {
        CheckDisposedCancelletionToken();
        Router.Navigate.Execute(_serviceProvider.GetRequiredService<EventJournalViewModel>());
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
