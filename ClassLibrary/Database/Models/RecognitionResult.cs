using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ClassLibrary.Database.Models;

public class RecognitionResult
{
    public int Id { get; set; }

    public required string ClassName { get; set; }

    public required int X { get; set; }

    public required int Y { get; set; }

    public required int Width { get; set; }

    public required int Height { get; set; }

    public required List<float> Nose { get; set; }
    public required List<float> LeftEye { get; set; }
    public required List<float> RightEye { get; set; }
    public required List<float> LeftEar { get; set; }
    public required List<float> RightEar { get; set; }
    public required List<float> LeftShoulder { get; set; }
    public required List<float> RightShoulder { get; set; }
    public required List<float> LeftElbow { get; set; }
    public required List<float> RightElbow { get; set; }
    public required List<float> LeftWrist { get; set; }
    public required List<float> RightWrist { get; set; }
    public required List<float> LeftHip { get; set; }
    public required List<float> RightHip { get; set; }
    public required List<float> LeftKnee { get; set; }
    public required List<float> RightKnee { get; set; }
    public required List<float> LeftAnkle { get; set; }
    public required List<float> RightAnkle { get; set; }
}
