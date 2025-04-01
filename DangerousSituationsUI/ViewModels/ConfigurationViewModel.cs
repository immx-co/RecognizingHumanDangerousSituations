using DangerousSituationsUI.Services;
using MsBox.Avalonia;
using ReactiveUI;
using Serilog;
using System.Reactive;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace DangerousSituationsUI.ViewModels;

public class ConfigurationViewModel : ReactiveObject, IRoutableViewModel
{
    #region Private Fields
    private string _connectionString;

    private string _url;

    private int _neuralWatcherTimeout;

    private int _frameRate;

    private readonly ConfigurationService _configurationService;
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
    #endregion

    #region Constructors
    public ConfigurationViewModel(IScreen screen, ConfigurationService configurationService)
    {
        HostScreen = screen;
        _configurationService = configurationService;

        ConnectionString = _configurationService.GetConnectionString("dbStringConnection");
        Url = _configurationService.GetConnectionString("srsStringConnection");
        NeuralWatcherTimeout = _configurationService.GetNeuralWatcherTimeout();
        FrameRate = _configurationService.GetFrameRate();

        SaveConfigCommand = ReactiveCommand.CreateFromTask(SaveConfig);
    }
    #endregion

    #region Private Methods
    private async Task SaveConfig()
    {
        try
        {
            Log.Information("Save Configuration: Start");
            Log.Debug("ConfigurationViewModel.SaveConfig: Start");
            await _configurationService.UpdateAppSettingsAsync(appSettings =>
            {
                appSettings.ConnectionStrings.dbStringConnection = ConnectionString;
                appSettings.ConnectionStrings.srsStringConnection = Url;
                appSettings.NeuralWatcherTimeout = NeuralWatcherTimeout;
                appSettings.FrameRate.Value = FrameRate;
            });

            ShowMessageBox("Success", $"Конфигурация успешно сохранена!");
            Log.Information("Save Configuration: Done");
            Log.Debug("ConfigurationViewModel.SaveConfig: Done");
        }
        catch (Exception ex)
        {
            ShowMessageBox("Failed", "Возникла ошибка при сохранении конфигурации.");
            Log.Warning("ConfigurationViewModel.SaveConfig: Error; Message: Возникла ошибка при сохранении конфигурации.");
        }
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
