using Avalonia.Media.Imaging;
using Avalonia.Collections;
using ClassLibrary.Services;
using ClassLibrary.Database.Models;

namespace ClassLibrary.Datacontracts;

public class FrameNDetections
{
    public BitmapModel Frame { get; set; }

    public AvaloniaList<RectItem> Detections { get; set; }

    public AvaloniaList<FigItem> Figs { get; set; }
}
