using Avalonia.Media.Imaging;
using ClassLibrary.Database.Models;

namespace ClassLibrary.Services;

public class RectItemService
{
    public RectItem InitRect(RecognitionResult recognitionResult, Bitmap file)
    {
        double widthImage = file.Size.Width;
        double heightImage = file.Size.Height;

        double k1 = widthImage / 800;
        double k2 = heightImage / 400;

        if (k1 > k2)
        {
            widthImage /= k1;
            heightImage /= k1;
        }
        else
        {
            widthImage /= k2;
            heightImage /= k2;
        }

        double xCenter = widthImage * (recognitionResult.X / file.Size.Width) + (800 - widthImage) / 2;
        double yCenter = heightImage * (recognitionResult.Y / file.Size.Height) + (400 - heightImage) / 2;

        int width = (int)(widthImage * (recognitionResult.Width / file.Size.Width));
        int height = (int)(heightImage * (recognitionResult.Height / file.Size.Height));

        int x = (int)(xCenter - width / 2);
        int y = (int)(yCenter - height / 2);

        string color = recognitionResult.ClassName switch
        {
            "human" => "Green",
            "wind/sup-board" => "Red",
            "bouy" => "Blue",
            "sailboat" => "Yellow",
            "kayak" => "Purple"
        };

        return new RectItem
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Color = color
        };
    }
}
