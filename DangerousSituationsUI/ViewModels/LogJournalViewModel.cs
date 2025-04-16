using ClassLibrary.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MsBox.Avalonia;
using ReactiveUI;
using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace DangerousSituationsUI.ViewModels;

public class LogJournalViewModel : ReactiveObject, IRoutableViewModel
{

    #region Public Fields

    #endregion

    #region View Model Settings
    public IScreen HostScreen { get; }

    public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

    public CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    #endregion

    #region Contructor
    public LogJournalViewModel(IScreen screen)
    {
        HostScreen = screen;

    }
    #endregion
}
