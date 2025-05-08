using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public List<float> Nose { get; set; }
    public List<float> LeftEye { get; set; }
    public List<float> RightEye { get; set; }
    public List<float> LeftEar { get; set; }
    public List<float> RightEar { get; set; }
    public List<float> LeftShoulder { get; set; }
    public List<float> RightShoulder { get; set; }
    public List<float> LeftElbow { get; set; }
    public List<float> RightElbow { get; set; }
    public List<float> LeftWrist { get; set; }
    public List<float> RightWrist { get; set; }
    public List<float> LeftHip { get; set; }
    public List<float> RightHip { get; set; }
    public List<float> LeftKnee { get; set; }
    public List<float> RightKnee { get; set; }
    public List<float> LeftAnkle { get; set; }
    public List<float> RightAnkle { get; set; }
}
