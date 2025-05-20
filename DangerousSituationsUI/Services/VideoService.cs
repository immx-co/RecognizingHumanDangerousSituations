using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ClassLibrary.Database.Models;
using ClassLibrary.Services;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DangerousSituationsUI.Services;

public class VideoService
{
    private readonly ConfigurationService _configurationService;

    public VideoService(ConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    public async Task<List<BitmapModel>> GetFramesAsync(IStorageFile file)
    {
        var bitmapImages = new List<BitmapModel>();
        var capture = new VideoCapture(file.Path.LocalPath);
        var image = new Mat();

        double fps = capture.Fps;
        int frameRate = _configurationService.GetFrameRate();
        int i = 0;
        await Task.Run(() =>
        {
            while (capture.IsOpened())
            {
                i++;
                capture.Read(image);
                if (image.Empty()) break;

                TimeSpan frameTime = TimeSpan.FromSeconds(i / fps);
                System.Drawing.Bitmap frame = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image);
                if (i % frameRate == 0) bitmapImages.Add(ConvertBitmapToAvalonia(frame,frameTime));
            }
        });
        return bitmapImages;
    }

    public async Task<List<BitmapModel>> GetFramesFromClipAndJsonAsync(string clipPath, VideoExportItem exportItem)
    {
        var result = new List<BitmapModel>();
        var capture = new VideoCapture(clipPath);

        if (!capture.IsOpened())
            return result;

        var timestamps = exportItem.Items
            .Select(i => i.FrameTime)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        await Task.Run(() =>
        {
            foreach (var timestamp in timestamps)
            {
                capture.Set(VideoCaptureProperties.PosMsec, timestamp.TotalMilliseconds);
                using var image = new Mat();
                capture.Read(image);
                if (image.Empty()) continue;

                using var bmp = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image);
                result.Add(ConvertBitmapToAvalonia(bmp, timestamp));
            }
        });

        return result;
    }

    private BitmapModel ConvertBitmapToAvalonia(System.Drawing.Bitmap bitmap, TimeSpan frameTime)
    {
        using (MemoryStream memory = new MemoryStream())
        {
            bitmap.Save(memory, ImageFormat.Png);
            memory.Position = 0;

            return new BitmapModel
            {
                frame = new Bitmap(memory),
                timeSpan = frameTime
            };
        }
    }
}
