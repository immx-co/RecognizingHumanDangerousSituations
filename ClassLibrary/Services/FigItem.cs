namespace ClassLibrary.Services;
using Avalonia;
using System.Text.Json.Serialization;

public class FigItem
{
    [JsonPropertyName("nose")]
    public List<float> Nose { get; set; } = new();

    [JsonPropertyName("left_eye")]
    public List<float> LeftEye { get; set; } = new();

    [JsonPropertyName("right_eye")]
    public List<float> RightEye { get; set; } = new();

    [JsonPropertyName("left_ear")]
    public List<float> LeftEar { get; set; } = new();

    [JsonPropertyName("right_ear")]
    public List<float> RightEar { get; set; } = new();

    [JsonPropertyName("left_shoulder")]
    public List<float> LeftShoulder { get; set; } = new();

    [JsonPropertyName("right_shoulder")]
    public List<float> RightShoulder { get; set; } = new();

    [JsonPropertyName("left_elbow")]
    public List<float> LeftElbow { get; set; } = new();

    [JsonPropertyName("right_elbow")]
    public List<float> RightElbow { get; set; } = new();

    [JsonPropertyName("left_wrist")]
    public List<float> LeftWrist { get; set; } = new();

    [JsonPropertyName("right_wrist")]
    public List<float> RightWrist { get; set; } = new();

    [JsonPropertyName("left_hip")]
    public List<float> LeftHip { get; set; } = new();

    [JsonPropertyName("right_hip")]
    public List<float> RightHip { get; set; } = new();

    [JsonPropertyName("left_knee")]
    public List<float> LeftKnee { get; set; } = new();

    [JsonPropertyName("right_knee")]
    public List<float> RightKnee { get; set; } = new();

    [JsonPropertyName("left_ankle")]
    public List<float> LeftAnkle { get; set; } = new();

    [JsonPropertyName("right_ankle")]
    public List<float> RightAnkle { get; set; } = new();

    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

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
