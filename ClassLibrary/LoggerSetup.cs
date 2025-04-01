using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary;

public class LoggerSetup
{
    public static ILogger CreateLogger()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File("logs/verbose.log", restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose)
            .WriteTo.File("logs/debug.log", restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
            .WriteTo.File("logs/info.log", restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
            .WriteTo.File("logs/warning.log", restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning)
            .WriteTo.File("logs/error.log", restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error)
            .WriteTo.File("logs/fatal.log", restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Fatal)
            .WriteTo.Console()
            .CreateLogger();
    }
}
