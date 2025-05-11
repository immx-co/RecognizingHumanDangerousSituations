using ClassLibrary.Database.Models;
using ClassLibrary.Services;
using System;

namespace DangerousSituationsUI.Services;

public class FigItemService
{
    public FigItem InitFig(RecognitionResult figure, Avalonia.Size size)
    {

        float canvasWidth = 800;
        float canvasHeight = 400;

        float imageWidth = (float)size.Width;
        float imageHeight = (float)size.Height;

        float scaleX = canvasWidth / imageWidth;
        float scaleY = canvasHeight / imageHeight;

        float scale = Math.Min(scaleX, scaleY);

        float offsetX = (canvasWidth - imageWidth * scale) / 2;
        float offsetY = (canvasHeight - imageHeight * scale) / 2;

        return new FigItem
        {
            Nose = [figure.Nose[0] * scale + offsetX, figure.Nose[1] * scale + offsetY],
            LeftEye = [figure.LeftEye[0] * scale + offsetX, figure.LeftEye[1] * scale + offsetY],
            RightEye = [figure.RightEye[0] * scale + offsetX, figure.RightEye[1] * scale + offsetY],
            LeftEar = [figure.LeftEar[0] * scale + offsetX, figure.LeftEar[1] * scale + offsetY],
            RightEar = [figure.RightEar[0] * scale + offsetX, figure.RightEar[1] * scale + offsetY],
            LeftShoulder = [figure.LeftShoulder[0] * scale + offsetX, figure.LeftShoulder[1] * scale + offsetY],
            RightShoulder = [figure.RightShoulder[0] * scale + offsetX, figure.RightShoulder[1] * scale + offsetY],
            LeftElbow = [figure.LeftElbow[0] * scale + offsetX, figure.LeftElbow[1] * scale + offsetY],
            RightElbow = [figure.RightElbow[0] * scale + offsetX, figure.RightElbow[1] * scale + offsetY],
            LeftWrist = [figure.LeftWrist[0] * scale + offsetX, figure.LeftWrist[1] * scale + offsetY],
            RightWrist = [figure.RightWrist[0] * scale + offsetX, figure.RightWrist[1] * scale + offsetY],
            LeftHip = [figure.LeftHip[0] * scale + offsetX, figure.LeftHip[1] * scale + offsetY],
            RightHip = [figure.RightHip[0] * scale + offsetX, figure.RightHip[1] * scale + offsetY],
            LeftKnee = [figure.LeftKnee[0] * scale + offsetX, figure.LeftKnee[1] * scale + offsetY],
            RightKnee = [figure.RightKnee[0] * scale + offsetX, figure.RightKnee[1] * scale + offsetY],
            LeftAnkle = [figure.LeftAnkle[0] * scale + offsetX, figure.LeftAnkle[1] * scale + offsetY],
            RightAnkle = [figure.RightAnkle[0] * scale + offsetX, figure.RightAnkle[1] * scale + offsetY],
            Color = figure.ClassName == "Standing" ? "Green" : "Red"
        };
    }
}
