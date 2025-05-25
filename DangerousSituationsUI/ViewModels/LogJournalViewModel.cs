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
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using Serilog.Events;
using System.Diagnostics;
using Telegram.Bot.Types;
using System.Linq;
using DynamicData;

namespace DangerousSituationsUI.ViewModels;

public class LogJournalViewModel : ReactiveObject, IRoutableViewModel
{
    IServiceProvider _serviceProvider;

    #region Private Fields
    private List<LogModel> _logs; 
    private HashSet<LogEventLevel> _selectedLevels = new();
    private List<LogModel> _allLogs = new();

    private static readonly Dictionary<string, LogEventLevel> LogLevelMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["VRB"] = LogEventLevel.Verbose,
        ["DBG"] = LogEventLevel.Debug,
        ["INF"] = LogEventLevel.Information,
        ["WRN"] = LogEventLevel.Warning,
        ["ERR"] = LogEventLevel.Error,
        ["FTL"] = LogEventLevel.Fatal
    };
    #endregion

    #region View Model Settings
    public IScreen HostScreen { get; }

    public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

    public CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    #endregion

    #region Public Fields
    public List<LogModel> Logs
    {
        get => _logs;
        set => this.RaiseAndSetIfChanged(ref _logs, value);
    }
    #endregion

    #region Public Methods
    public void ClearUI()
    {
        Logs = new();
    }
    public async Task ResetUIAsync()
    {
        await LoadAsync();
        UpdateFilteredLogs();
    }

    #endregion

    #region Commands
    public ReactiveCommand<LogEventLevel, Unit> ToggleLevelFilterCommand { get; }
    public ReactiveCommand<Unit, Unit> UpdateUI { get; }
    public ReactiveCommand<Unit, Unit> DeleteAllLogsCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteFilteredLogsCommand { get; }
    #endregion

    #region Contructor
    public LogJournalViewModel(IScreen screen, IServiceProvider serviceProvider)
    {
        HostScreen = screen;
        _serviceProvider = serviceProvider;

        _ = LoadAsync();

        ToggleLevelFilterCommand = ReactiveCommand.Create<LogEventLevel>(ToggleLevelFilter);
        UpdateUI = ReactiveCommand.CreateFromTask(ResetUIAsync);
        DeleteAllLogsCommand = ReactiveCommand.CreateFromTask(DeleteAllLogsAsync);
        DeleteFilteredLogsCommand = ReactiveCommand.CreateFromTask(DeleteFilteredLogsAsync);

    }
    #endregion

    #region Private Methods
    private async Task LoadAsync()
    {
        var logs = new List<LogModel>();
        logs.AddRange(await ReadLogsFromFileAsync("logs/verbose.log"));
        logs.AddRange(await ReadLogsFromFileAsync("logs/debug.log"));
        logs.AddRange(await ReadLogsFromFileAsync("logs/info.log"));
        logs.AddRange(await ReadLogsFromFileAsync("logs/warning.log"));
        logs.AddRange(await ReadLogsFromFileAsync("logs/error.log"));
        logs.AddRange(await ReadLogsFromFileAsync("logs/fatal.log"));

        _allLogs = logs;
        Logs = new List<LogModel>(_allLogs.OrderBy(log => log.Time));
    }

    private void ToggleLevelFilter(LogEventLevel level)
    {
        if (_selectedLevels.Contains(level))
        {
            _selectedLevels.Remove(level);
        }
        else
        {
            _selectedLevels.Add(level);
        }

        UpdateFilteredLogs();
    }

    private void UpdateFilteredLogs()
    {
        if (_selectedLevels.Count == 0)
        {
            Logs = new();
            return;
        }

        Logs = new List<LogModel>(
            _allLogs.Where(log => _selectedLevels.Contains(log.Level)).OrderBy(log => log.Time)
        );
    }

    private async Task<List<LogModel>> ReadLogsFromFileAsync(string filePath)
    {
        var logs = new List<LogModel>();

        if (!File.Exists(filePath))
            return logs;

        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);
            string? line;
            while ((line = await sr.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var match = Regex.Match(line,
                    @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+-]\d{2}:\d{2})\s\[(\w+)\]\s(.+)$");

                if (!match.Success) continue;

                if (!DateTimeOffset.TryParse(match.Groups[1].Value, out var timestamp)) continue;

                if (!LogLevelMap.TryGetValue(match.Groups[2].Value, out var level))
                    level = LogEventLevel.Information;

                logs.Add(new LogModel
                {
                    Time = timestamp,
                    Level = level,
                    Text = match.Groups[3].Value
                });
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed reading log: {ex.Message}");
        }

        return logs;
    }


    public async Task DeleteAllLogsAsync()
    {
        string[] logFiles = new[]
        {
        "logs/verbose.log",
        "logs/debug.log",
        "logs/info.log",
        "logs/warning.log",
        "logs/error.log",
        "logs/fatal.log"
        };

        foreach (var file in logFiles)
        {
            try
            {
                ClearFileAsync(file);
            }
            catch (Exception ex)
            {
                Log.Error($"Error truncating file {file}");
            }

        }
        await ConfigureLoggingAsync();
        await ResetUIAsync();
    }


    public async Task DeleteFilteredLogsAsync()
    {
        foreach (var level in _selectedLevels)
        {
            string? filePath = level switch
            {
                LogEventLevel.Verbose => "logs/verbose.log",
                LogEventLevel.Debug => "logs/debug.log",
                LogEventLevel.Information => "logs/info.log",
                LogEventLevel.Warning => "logs/warning.log",
                LogEventLevel.Error => "logs/error.log",
                LogEventLevel.Fatal => "logs/fatal.log",
                _ => null
            };

            if (filePath == null) continue;

            try
            {
                ClearFileAsync(filePath);
            }
            catch (Exception ex)
            {
                Log.Error($"Error truncating file {filePath}");
            }

        }
        await ConfigureLoggingAsync();
        await ResetUIAsync();
    }

    private async Task ClearFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Log.Error($"File not found: {Path.GetFullPath(filePath)}");
                return;
            }

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            fs.SetLength(0);
            Log.Information($"Cleared file: {filePath}");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to clear file {filePath}: {ex.Message}");
        }
    }

    private async Task ConfigureLoggingAsync()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Information)
                                  .WriteTo.File(@"Logs\Info.log", shared: true))
            .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Debug)
                                  .WriteTo.File(@"Logs\Debug.log", shared: true))
            .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Warning)
                                  .WriteTo.File(@"Logs\Warning.log", shared: true))
            .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Error)
                                  .WriteTo.File(@"Logs\Error.log", shared: true))
            .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Fatal)
                                  .WriteTo.File(@"Logs\Fatal.log", shared: true))
            .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Verbose)
                                  .WriteTo.File(@"Logs\Verbose.log", shared: true))
            .CreateLogger();
    }


    #endregion

    #region Classes
    public class LogModel
    {
        public DateTimeOffset Time { get; set; }
        public LogEventLevel Level { get; set; }
        public string Text { get; set; }

        public override string ToString()
        {
            return $"{Time} [{Level}] {Text}";
        }
    }

    #endregion

}



