using ClassLibrary.Database.Models;
using ClassLibrary.Database;
using ClassLibrary.Datacontracts;
using ClassLibrary.Services;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using DangerousSituationsUI.ViewModels;

namespace SignalRApplication.Hubs;

public class HotificationHub : Hub
{
    IServiceProvider _serviceProvider;
    ConfigurationService _configurationService;

    public HotificationHub(IServiceProvider serviceProvider, ConfigurationService configurationService)
    {
        _serviceProvider = serviceProvider;
        _configurationService = configurationService;
    }

    public async Task SaveConfig(
        string сonnectionString, 
        string url, 
        int neuralWatcherTimeout, 
        int frameRate, 
        int frameScrollTimeout)
    {
        Log.Debug("HotificationHub.SaveConfig: Start.");
        LogJournalViewModel.logString += "HotificationHub.SaveConfig: Start.\n";
        try
        {
            await _configurationService.UpdateAppSettingsAsync(appSettings =>
            {
                appSettings.ConnectionStrings.dbStringConnection = сonnectionString;
                appSettings.ConnectionStrings.srsStringConnection = url;
                appSettings.NeuralWatcherTimeout = neuralWatcherTimeout;
                appSettings.FrameRate.Value = frameRate;
                appSettings.FrameScrollTimeout.Value = frameScrollTimeout;
            });

            await Clients.Caller.SendAsync("SaveConfigOk", _configurationService.GetConnectionString("dbStringConnection"), 
                _configurationService.GetConnectionString("srsStringConnection"),
                _configurationService.GetNeuralWatcherTimeout(), 
                _configurationService.GetFrameRate(),
                _configurationService.GetFrameScrollTimeout());
            Log.Debug("HotificationHub.SaveConfig: Done.");
            LogJournalViewModel.logString += "HotificationHub.SaveConfig: Done.\n";
            return;
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("SaveConfigFailed");
            return;
        }
    }
}
