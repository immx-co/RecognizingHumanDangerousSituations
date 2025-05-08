using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary.Database.Models;

public class Frame
{
    public Guid FrameId { get; set; }

    public Guid VideoId { get; set; }

    public byte[] FrameData { get; set; }

    public DateTime CreatedAt { get; set; }

    public Video Video { get; set; }

    public List<Detection> Detections { get; set; } = new List<Detection>();

    public TimeSpan FrameTime { get; set; }
}
