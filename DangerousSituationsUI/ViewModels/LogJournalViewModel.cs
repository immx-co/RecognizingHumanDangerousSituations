using ClassLibrary;
using ClassLibrary.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MsBox.Avalonia;
using ReactiveUI;
using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace DangerousSituationsUI.ViewModels;

public class LogJournalViewModel : ReactiveObject, IRoutableViewModel
{

    IServiceProvider _serviceProvider;

    #region Private Fields

    private string _logString;

    #endregion

    #region View Model Settings
    public IScreen HostScreen { get; }

    public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

    public CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    #endregion

    #region Properties
    public string LogString
    {
        get => _logString;
        set => this.RaiseAndSetIfChanged(ref _logString, value);
    }

    #endregion

    #region Contructor
    public LogJournalViewModel(IScreen screen, IServiceProvider serviceProvider)
    {
        HostScreen = screen;
        _serviceProvider = serviceProvider;
        LogString += Log.Logger.ToString();

    }
    #endregion

    #region Public Methodes

    public void AddLogString(string log)
    {
        LogString += log;
    }

    #endregion

}
