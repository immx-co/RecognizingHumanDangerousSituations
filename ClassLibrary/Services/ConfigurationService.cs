using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ClassLibrary.Services;

public class ConfigurationService
{
    #region Private Fields
    private readonly IConfiguration _configuration;

    private string dbStringConnection;

    private string srsStringConnection;

    private int NeuralWatcherTimeout;

    private int FrameRate;
    #endregion

    #region Constructor
    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;

        dbStringConnection = _configuration.GetConnectionString("dbStringConnection");
        srsStringConnection = _configuration.GetConnectionString("srsStringConnection");
        NeuralWatcherTimeout = _configuration.GetSection("NeuralWatcherTimeout").Get<int>();
        FrameRate = Convert.ToInt32(_configuration.GetSection("FrameRate:Value").Value);
    }
    #endregion

    #region Public Methods
    public string? GetConnectionString(string name)
    {
        if (name == "dbStringConnection")
        {
            return dbStringConnection;
        }
        else if (name == "srsStringConnection")
        {
            return srsStringConnection;
        }
        else
        {
            return null;
        }
    }

    public int GetNeuralWatcherTimeout()
    {
        return NeuralWatcherTimeout;
    }

    public int GetFrameRate()
    {
        return FrameRate;
        ;
    }

    public async Task UpdateAppSettingsAsync(Action<AppSettings> updateAction)
    {
        var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\..\\DangerousSituationsUI", "appsettings.json");

        var json = await File.ReadAllTextAsync(appSettingsPath);

        var appSettings = JsonSerializer.Deserialize<AppSettings>(json);

        updateAction(appSettings);

        var updatedJson = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(appSettingsPath, updatedJson);

        dbStringConnection = appSettings.ConnectionStrings.dbStringConnection;
        srsStringConnection = appSettings.ConnectionStrings.srsStringConnection;
        NeuralWatcherTimeout = appSettings.NeuralWatcherTimeout;
        FrameRate = appSettings.FrameRate.Value;
    }
    #endregion
}
