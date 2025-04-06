namespace ClassLibrary.Database.Models;

public class Detection
{
    public Guid DetectionId { get; set; }

    public Guid FrameId { get; set; }

    public string ClassName { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }
}
