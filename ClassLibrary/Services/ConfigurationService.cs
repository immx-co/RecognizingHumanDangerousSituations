using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ClassLibrary.Services;

public class ConfigurationService
{
    #region Private Properties
    private readonly IConfiguration _configuration;
    #endregion

    #region Public Properties
    public string DbStringConnection { get; set; }

    public string SrsStringConnection { get; set; }

    public int NeuralWatcherTimeout { get; set; }

    public int FrameRate { get; set; }
    #endregion

    #region Constructor
    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;

        DbStringConnection = _configuration.GetConnectionString("dbStringConnection");
        SrsStringConnection = _configuration.GetConnectionString("srsStringConnection");
        NeuralWatcherTimeout = _configuration.GetSection("NeuralWatcherTimeout").Get<int>();
        FrameRate = Convert.ToInt32(_configuration.GetSection("FrameRate:Value").Value);
    }
    #endregion

    #region Public Methods
    public string? GetConnectionString(string name)
    {
        if (name == "dbStringConnection")
        {
            return DbStringConnection;
        }
        else if (name == "srsStringConnection")
        {
            return SrsStringConnection;
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
    }

    public async Task UpdateAppSettingsAsync(Action<AppSettings> updateAction)
    {
        var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\..\\DangerousSituationsUI", "appsettings.json");

        var json = await File.ReadAllTextAsync(appSettingsPath);

        var appSettings = JsonSerializer.Deserialize<AppSettings>(json);

        updateAction(appSettings);

        var updatedJson = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(appSettingsPath, updatedJson);

        DbStringConnection = appSettings.ConnectionStrings.dbStringConnection;
        SrsStringConnection = appSettings.ConnectionStrings.srsStringConnection;
        NeuralWatcherTimeout = appSettings.NeuralWatcherTimeout;
        FrameRate = appSettings.FrameRate.Value;
    }
    #endregion
}
