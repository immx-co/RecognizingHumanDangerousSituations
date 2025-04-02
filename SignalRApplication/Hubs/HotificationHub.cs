using ClassLibrary.Services;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using System;

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

    public async Task SaveConfig(string сonnectionString, string url, int neuralWatcherTimeout, int frameRate)
    {
        Log.Debug("HotificationHub.SaveConfig: Start.");
        try
        {
            await _configurationService.UpdateAppSettingsAsync(appSettings =>
            {
                appSettings.ConnectionStrings.dbStringConnection = сonnectionString;
                appSettings.ConnectionStrings.srsStringConnection = url;
                appSettings.NeuralWatcherTimeout = neuralWatcherTimeout;
                appSettings.FrameRate.Value = frameRate;
            });

            await Clients.Caller.SendAsync("SaveConfigOk");
            Log.Debug("HotificationHub.SaveConfig: Done.");
            return;
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("SaveConfigFailed");
            return;
        }
    }
}
