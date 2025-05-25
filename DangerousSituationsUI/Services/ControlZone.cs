using Avalonia.Media.TextFormatting.Unicode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DangerousSituationsUI.Services;

public class ControlZone
{
    public required string Description { get; set; }

    public required List<ZonePoint> Points { get; set; }
}

public class ZonePoint
{
    public double X { get; set; }

    public double Y { get; set; }
}
