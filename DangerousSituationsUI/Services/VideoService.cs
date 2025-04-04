using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace DangerousSituationsUI.Services;

public class VideoService
{
    private readonly ConfigurationService _configurationService;

    public VideoService(ConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    public async Task<List<Bitmap>> GetFramesAsync(IStorageFile file)
    {
        var bitmapImages = new List<Bitmap>();
        var capture = new VideoCapture(file.Path.LocalPath);
        var image = new Mat();

        int frameRate = _configurationService.GetFrameRate();
        int i = 0;
        await Task.Run(() =>
        {
            while (capture.IsOpened())
            {
                i++;
                capture.Read(image);
                if (image.Empty()) break;
                System.Drawing.Bitmap frame = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image);
                if (i % frameRate == 0) bitmapImages.Add(ConvertBitmapToAvalonia(frame));
            }
        });
        return bitmapImages;
    }

    private Bitmap ConvertBitmapToAvalonia(System.Drawing.Bitmap bitmap)
    {
        using (MemoryStream memory = new MemoryStream())
        {
            bitmap.Save(memory, ImageFormat.Png);
            memory.Position = 0;

            return new Bitmap(memory);
        }
    }
}
