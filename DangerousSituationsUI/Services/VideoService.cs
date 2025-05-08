using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ClassLibrary.Database.Models;
using ClassLibrary.Services;
using DangerousSituationsUI.ViewModels;
using DangerousSituationsUI.Views;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using static DangerousSituationsUI.ViewModels.MainViewModel;

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
