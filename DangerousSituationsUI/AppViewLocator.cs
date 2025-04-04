using DangerousSituationsUI.ViewModels;
using DangerousSituationsUI.Views;
using System;
using ReactiveUI;

namespace DangerousSituationsUI;

public class AppViewLocator : IViewLocator
{
    public IViewFor ResolveView<T>(T viewModel, string contract = null) => viewModel switch
    {
        MainViewModel context => new MainView { ViewModel = context },
        VideoEventJournalViewModel context => new VideoEventJournalView { ViewModel = context },
        ConfigurationViewModel context => new ConfigurationView { ViewModel = context },
        _ => throw new ArgumentOutOfRangeException(nameof(viewModel))
    };
}
