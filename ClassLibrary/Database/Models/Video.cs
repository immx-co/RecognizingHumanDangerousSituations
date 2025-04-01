using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary.Database.Models;

public class Video
{
    public Guid VideoId { get; set; }

    public string VideoName { get; set; }

    public string FilePath { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<Frame> Frames { get; set; } = new List<Frame>();
}
