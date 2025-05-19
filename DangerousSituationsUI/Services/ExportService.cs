using ClassLibrary.Database;
using ClassLibrary.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FFMpegCore;
using Microsoft.EntityFrameworkCore;
using System.Reactive.Linq;
using Avalonia.Collections;
using System.Collections.Generic;
using ClassLibrary.Database.Models;
using Avalonia.Media.Imaging;
using SkiaSharp;


namespace DangerousSituationsUI.Services
{
    public class ExportService
    {
        private readonly IServiceProvider _serviceProvider;

        public ExportService(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public async Task<(string, string)?> ExportClipAndDetectionsAsync(
            Guid videoId,
            string originalVideoPath,
            TimeSpan startTime,
            TimeSpan endTime,
            FilesService filesService)
        {
            string suggestedName = $"event_{Path.GetFileNameWithoutExtension(originalVideoPath)}.avi";
            string? outputPath = await filesService.PickExportPathAsync(suggestedName);

            if (string.IsNullOrEmpty(outputPath))
                return null;

            string clipPath = await CutClipAsync(originalVideoPath, outputPath, startTime, endTime);
            var detections = await GetClipDetectionsAsync(videoId, startTime, endTime);

            string jsonPath = Path.ChangeExtension(clipPath, ".json");

            var options = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(detections, options));

            return (clipPath, jsonPath);
        }


        public async Task<Guid?> GetVideoIdByNameAsync(string videoName)
        {
            using var db = _serviceProvider.GetRequiredService<ApplicationContext>();

            return await db.Videos
                .Where(v => v.VideoName == videoName)
                .Select(v => (Guid?)v.VideoId)
                .FirstOrDefaultAsync();
        }

        private async Task<string> CutClipAsync(string originalPath, string outputPath, TimeSpan startTime, TimeSpan endTime)
        {
            string ffmpegFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFMpeg");

            GlobalFFOptions.Configure(new FFOptions
            {
                BinaryFolder = ffmpegFolder
            });

            await FFMpeg.SubVideoAsync(originalPath, outputPath, startTime, endTime);

            return outputPath;
        }


        private async Task<List<JsonItem>> GetClipDetectionsAsync(Guid videoId, TimeSpan startTime, TimeSpan endTime)
        {
            using var db = _serviceProvider.GetRequiredService<ApplicationContext>();

            var dbVideo = await db.Videos
                .FirstOrDefaultAsync(v => v.VideoId == videoId);

            if (dbVideo == null)
                return new();

            var frames = await db.Frames
                .Where(f => f.VideoId == dbVideo.VideoId &&
                            f.FrameTime >= startTime &&
                            f.FrameTime <= endTime)
                .ToListAsync();

            var frameIds = frames.Select(f => f.FrameId).ToList();
            var frameTimeMap = frames.ToDictionary(f => f.FrameId, f => f.FrameTime);

            var detections = await db.Detections
                .Where(d => frameIds.Contains(d.FrameId))
                .ToListAsync();

            var grouped = detections.GroupBy(d => d.FrameId);

            var jsonItems = new List<JsonItem>();

            foreach (var group in grouped)
            {
                var frameId = group.Key;
                if (!frameTimeMap.TryGetValue(frameId, out var frameTime))
                    continue;

                var rectItems = new AvaloniaList<RectItem>();
                var figItems = new AvaloniaList<FigItem>();

                foreach (var det in group)
                {
                    rectItems.Add(new RectItem
                    {
                        X = det.X,
                        Y = det.Y,
                        Width = det.Width,
                        Height = det.Height,
                        Color = det.ClassName switch
                        {
                            "Standing" => "Green",
                            "Lying" => "Red",
                            _ => "Yellow"
                        }
                    });

                    figItems.Add(new FigItem
                    {
                        Nose = det.Nose,
                        LeftEye = det.LeftEye,
                        RightEye = det.RightEye,
                        LeftEar = det.LeftEar,
                        RightEar = det.RightEar,
                        LeftShoulder = det.LeftShoulder,
                        RightShoulder = det.RightShoulder,
                        LeftElbow = det.LeftElbow,
                        RightElbow = det.RightElbow,
                        LeftWrist = det.LeftWrist,
                        RightWrist = det.RightWrist,
                        LeftHip = det.LeftHip,
                        RightHip = det.RightHip,
                        LeftKnee = det.LeftKnee,
                        RightKnee = det.RightKnee,
                        LeftAnkle = det.LeftAnkle,
                        RightAnkle = det.RightAnkle,
                        Color = det.ClassName switch
                        {
                            "Standing" => "Green",
                            "Lying" => "Red",
                            _ => "Blue"
                        }
                    });
                }

                jsonItems.Add(new JsonItem
                {
                    FrameTime = frameTime,
                    RectItems = rectItems,
                    FigItems = figItems
                });
            }

            return jsonItems.OrderBy(j => j.FrameTime).ToList();
        }

        public async Task<List<BitmapModel>> GetFramesForDetectionsAsync(string videoName, List<JsonItem> jsonItems)
        {
            using var db = _serviceProvider.GetRequiredService<ApplicationContext>();
            var videoId = await GetVideoIdByNameAsync(videoName);
            if (videoId == null) return new List<BitmapModel>();

            var jsonFrameTimes = jsonItems.Select(j => j.FrameTime).ToList();

            var allFrames = await db.Frames
                .Where(f => f.VideoId == videoId.Value)
                .ToListAsync();

            var matchedFrames = jsonFrameTimes
                .Select(time => allFrames.FirstOrDefault(f => f.FrameTime== time))
                .Where(f => f != null)
                .Distinct()
                .ToList();

            var bitmapModels = matchedFrames.Select(f =>
            {
                using var ms = new MemoryStream(f.FrameData);
                return new BitmapModel
                {
                    frame = new Bitmap(ms),
                    timeSpan = f.FrameTime
                };
            }).ToList();

            return bitmapModels;
        }

    }

}
