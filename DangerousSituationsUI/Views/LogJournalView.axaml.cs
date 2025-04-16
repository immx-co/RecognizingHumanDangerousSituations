using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DangerousSituationsUI.ViewModels;
using ReactiveUI;

namespace DangerousSituationsUI.Views;
public partial class LogJournalView : ReactiveUserControl<LogJournalViewModel>
{
    public LogJournalView()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}