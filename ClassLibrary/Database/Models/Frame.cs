namespace ClassLibrary.Database.Models;

public class Frame
{
    public Guid FrameId { get; set; }

    public Guid VideoId { get; set; }

    public byte[] FrameData { get; set; }

    public DateTime CreatedAt { get; set; }

    public Video Video { get; set; }

    public List<Detection> Detections { get; set; } = new List<Detection>();
}
