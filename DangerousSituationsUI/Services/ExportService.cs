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


namespace DangerousSituationsUI.Services
{
    public class ExportService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly FilesService _filesService;

        public ExportService(IServiceProvider serviceProvider, FilesService filesService)
        {
            _serviceProvider = serviceProvider;
            _filesService = filesService;
        }

        public async Task<(string, string)?> ExportClipAndDetectionsAsync(
            Guid videoId,
            string originalVideoPath,
            TimeSpan startTime,
            TimeSpan endTime)
        {
            string suggestedName = $"event_{Path.GetFileNameWithoutExtension(originalVideoPath)}.avi";
            string? outputPath = await _filesService.PickExportPathAsync(suggestedName);

            if (string.IsNullOrEmpty(outputPath))
                return null;

            string clipPath = await CutClipAsync(originalVideoPath, outputPath, startTime, endTime);

            var detections = await GetClipDetectionsAsync(videoId, startTime, endTime, normalizeToClip: true);

            string jsonPath = Path.ChangeExtension(clipPath, ".json");

            var options = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(detections, options));

            return (clipPath, jsonPath);
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

        private async Task<VideoExportItem> GetClipDetectionsAsync(
            Guid videoId,
            TimeSpan startTime,
            TimeSpan endTime,
            bool normalizeToClip)
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
                    FrameTime = normalizeToClip ? frameTime - startTime : frameTime,
                    RectItems = rectItems,
                    FigItems = figItems
                });
            }

            jsonItems = jsonItems.OrderBy(j => j.FrameTime).ToList();
            var videoExportItem = new VideoExportItem
            {
                ClipStart = startTime,
                Items = jsonItems
            };

            return videoExportItem;
        }

    }

}
