using Avalonia.Media.Imaging;
using Avalonia.Collections;
using ClassLibrary.Services;

namespace ClassLibrary.Datacontracts;

public class FrameNDetections
{
    public Bitmap Frame { get; set; }

    public AvaloniaList<RectItem> Detections { get; set; }

    public AvaloniaList<FigItem> Figs { get; set; }
}
