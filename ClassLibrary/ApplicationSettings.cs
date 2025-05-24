using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary;

public class AppSettings
{
    public ConnectionStringsConfig ConnectionStrings { get; set; }

    public int NeuralWatcherTimeout { get; set; }

    public FrameRate FrameRate { get; set; }

    public FrameScrollTimeout FrameScrollTimeout { get; set; }
}

public class ConnectionStringsConfig
{
    public string dbStringConnection { get; set; }

    public string srsStringConnection { get; set; }
}

public class FrameRate
{
    public int Value { get; set; }
}

public class FrameScrollTimeout
{
    public int Value { get; set; }
}