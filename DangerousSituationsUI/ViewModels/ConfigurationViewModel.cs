using ClassLibrary.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MsBox.Avalonia;
using ReactiveUI;
using Serilog;
using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace DangerousSituationsUI.ViewModels;

public class ConfigurationViewModel : ReactiveObject, IRoutableViewModel
{
    #region Private Fields
    private string _connectionString;

    private string _url;

    private int _neuralWatcherTimeout;

    private int _frameRate;

    private int _frameScrollTimeout;

    private readonly ConfigurationService _configurationService;

    HubConnectionWrapper _hubConnectionWrapper;
    #endregion

    #region View Model Settings
    public IScreen HostScreen { get; }

    public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

    public CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    #endregion

    #region Commands
    public ReactiveCommand<Unit, Unit> SaveConfigCommand { get; }
    #endregion

    #region Properties
    public string ConnectionString
    {
        get => _connectionString;
        set => this.RaiseAndSetIfChanged(ref _connectionString, value);
    }

    public string Url
    {
        get => _url;
        set => this.RaiseAndSetIfChanged(ref _url, value);
    }

    public int FrameRate
    {
        get => _frameRate;
        set => this.RaiseAndSetIfChanged(ref _frameRate, value);
    }

    public int NeuralWatcherTimeout
    {
        get => _neuralWatcherTimeout;
        set => this.RaiseAndSetIfChanged(ref _neuralWatcherTimeout, value);
    }

    public int FrameScrollTimeout
    {
        get => _frameScrollTimeout;
        set => this.RaiseAndSetIfChanged(ref _frameScrollTimeout, value);
    }
    #endregion

    #region Constructors
    public ConfigurationViewModel(IScreen screen, ConfigurationService configurationService, HubConnectionWrapper hubConnectionWrapper)
    {
        HostScreen = screen;
        _configurationService = configurationService;
        _hubConnectionWrapper = hubConnectionWrapper;

        ConnectionString = _configurationService.GetConnectionString("dbStringConnection");
        Url = _configurationService.GetConnectionString("srsStringConnection");
        NeuralWatcherTimeout = _configurationService.GetNeuralWatcherTimeout();
        FrameRate = _configurationService.GetFrameRate();
        FrameScrollTimeout = _configurationService.GetFrameScrollTimeout();

        SaveConfigCommand = ReactiveCommand.CreateFromTask(SaveConfig);

        _hubConnectionWrapper.Connection.On<string, string, int, int, int>("SaveConfigOk", (dbStringConnection, srsStringConnection, neuralWatcherTimeout, frameRate, frameScrollTimeout) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _configurationService.DbStringConnection = dbStringConnection;
                _configurationService.SrsStringConnection = srsStringConnection;
                _configurationService.NeuralWatcherTimeout = neuralWatcherTimeout;
                _configurationService.FrameRate = frameRate;
                _configurationService.FrameScrollTimeout = frameScrollTimeout;
                ShowMessageBox("Success", $"Конфигурация успешно сохранена!");
                return;
            });
        });

        _hubConnectionWrapper.Connection.On("SaveConfigFailed", () =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                ShowMessageBox("Failed", "Возникла ошибка при сохранении конфигурации.");
                return;
            });
        });
    }
    #endregion

    #region Private Methods
    private async Task SaveConfig()
    {
        await _hubConnectionWrapper.SaveConfig(ConnectionString, Url, NeuralWatcherTimeout, FrameRate, FrameScrollTimeout);
        Log.Information($"Save Configs: ConnectionString:{ConnectionString}, Url:{Url}, NeuralWatcherTimeout:{NeuralWatcherTimeout}, FrameRate:{FrameRate}, FrameScrollTimeout:{FrameScrollTimeout}");
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Показывает всплывающее сообщение.
    /// </summary>
    /// <param name="caption">Заголовок сообщения.</param>
    /// <param name="message">Сообщение пользователю.</param>
    public void ShowMessageBox(string caption, string message)
    {
        var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandard(caption, message);
        messageBoxStandardWindow.ShowAsync();
    }
    #endregion
}
