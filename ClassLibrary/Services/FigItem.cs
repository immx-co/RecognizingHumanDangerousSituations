namespace ClassLibrary.Services;
using Avalonia;

public class FigItem
{
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

    public string Color { get; set; }

    public Point NosePoint => ToPoint(Nose);
    public Point LeftEyePoint => ToPoint(LeftEye);
    public Point RightEyePoint => ToPoint(RightEye);
    public Point LeftEarPoint => ToPoint(LeftEar);
    public Point RightEarPoint => ToPoint(RightEar);
    public Point LeftShoulderPoint => ToPoint(LeftShoulder);
    public Point RightShoulderPoint => ToPoint(RightShoulder);
    public Point LeftElbowPoint => ToPoint(LeftElbow);
    public Point RightElbowPoint => ToPoint(RightElbow);
    public Point LeftWristPoint => ToPoint(LeftWrist);
    public Point RightWristPoint => ToPoint(RightWrist);
    public Point LeftHipPoint => ToPoint(LeftHip);
    public Point RightHipPoint => ToPoint(RightHip);
    public Point LeftKneePoint => ToPoint(LeftKnee);
    public Point RightKneePoint => ToPoint(RightKnee);
    public Point LeftAnklePoint => ToPoint(LeftAnkle);
    public Point RightAnklePoint => ToPoint(RightAnkle);

    private static Point ToPoint(List<float> coords)
    {
        return new Point(coords[0], coords[1]);
    }
}
