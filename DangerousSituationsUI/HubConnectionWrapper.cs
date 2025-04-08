using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClassLibrary.Datacontracts;

namespace DangerousSituationsUI;

public class HubConnectionWrapper
{
    #region Hub Settings
    public HubConnection Connection;

    public HubConnectionWrapper()
    {
        Connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:1234/notify")
                .Build();

        Connection.Closed += async (error) =>
        {
            await Task.Delay(new Random().Next(0, 5) * 1000);
            await Connection.StartAsync();
        };
    }

    public async Task Start()
    {
        await Connection.StartAsync();
    }

    public async Task Stop()
    {
        await Connection.StopAsync();
    }
    #endregion

    #region Configuration View Model
    public async Task SaveConfig(string connectionString, string url, int neuralWatcherTimeout, int frameRate)
    {
        await Connection.InvokeAsync("SaveConfig", connectionString, url, neuralWatcherTimeout, frameRate);
    }
    #endregion
}
