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
        _allLogs = new();
        _allLogs.AddRange(ReadLogsFromFile("logs/verbose.log"));
        _allLogs.AddRange(ReadLogsFromFile("logs/debug.log"));
        _allLogs.AddRange(ReadLogsFromFile("logs/info.log"));
        _allLogs.AddRange(ReadLogsFromFile("logs/warning.log"));
        _allLogs.AddRange(ReadLogsFromFile("logs/error.log"));
        _allLogs.AddRange(ReadLogsFromFile("logs/fatal.log"));
        _selectedLevels = new();
        Logs = new();
    }
    public void ResetUI()
    {
        _allLogs = new();
        _allLogs.AddRange(ReadLogsFromFile("logs/verbose.log"));
        _allLogs.AddRange(ReadLogsFromFile("logs/debug.log"));
        _allLogs.AddRange(ReadLogsFromFile("logs/info.log"));
        _allLogs.AddRange(ReadLogsFromFile("logs/warning.log"));
        _allLogs.AddRange(ReadLogsFromFile("logs/error.log"));
        _allLogs.AddRange(ReadLogsFromFile("logs/fatal.log"));
        Logs = new();
        UpdateFilteredLogs();
    }
    #endregion

    #region Contructor
    public LogJournalViewModel(IScreen screen, IServiceProvider serviceProvider)
    {
        HostScreen = screen;
        _serviceProvider = serviceProvider; 
        _allLogs = new();
        _allLogs.AddRange(ReadLogsFromFile("logs/verbose.log"));
        _allLogs.AddRange(ReadLogsFromFile("logs/debug.log"));
        _allLogs.AddRange(ReadLogsFromFile("logs/info.log"));
        _allLogs.AddRange(ReadLogsFromFile("logs/warning.log"));
        _allLogs.AddRange(ReadLogsFromFile("logs/error.log"));
        _allLogs.AddRange(ReadLogsFromFile("logs/fatal.log"));
        Logs = new();

        ToggleLevelFilterCommand = ReactiveCommand.Create<LogEventLevel>(ToggleLevelFilter);
        UpdateUI = ReactiveCommand.Create(ResetUI);
    }
    #endregion




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





    #region Private Methods
    private List<LogModel> ReadLogsFromFile(string filePath)
    {
        var logs = new List<LogModel>();

        if (!File.Exists(filePath))
        {
            Debug.WriteLine($"Log file not found: {filePath}");
            return logs;
        }

        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);

            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var match = Regex.Match(line,
                    @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+-]\d{2}:\d{2})\s\[(\w+)\]\s(.+)$");

                if (!match.Success)
                {
                    Debug.WriteLine($"Failed to parse log line: {line}");
                    continue;
                }

                if (!DateTimeOffset.TryParse(match.Groups[1].Value, out var timestamp))
                {
                    Debug.WriteLine($"Failed to parse timestamp: {match.Groups[1].Value}");
                    continue;
                }

                var levelString = match.Groups[2].Value;
                if (!LogLevelMap.TryGetValue(levelString, out var level))
                {
                    Debug.WriteLine($"Unknown log level: {levelString}");
                    level = LogEventLevel.Information;
                }

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
            Debug.WriteLine($"Error reading log file: {ex.Message}");
        }

        return logs;
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
    public ReactiveCommand<LogEventLevel, Unit> ToggleLevelFilterCommand { get; }
    public ReactiveCommand<Unit, Unit> UpdateUI { get; }
}



