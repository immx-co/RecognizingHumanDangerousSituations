using Avalonia.Collections;
using Avalonia.Platform.Storage;
using Avalonia.Media.Imaging;
using ClassLibrary.Database.Models;
using ClassLibrary.Services;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassLibrary.Database;

namespace SignalRApplication.Hubs;

public class HotificationHub : Hub
{
    #region Private Fields
    IServiceProvider _serviceProvider;

    ConfigurationService _configurationService;

    VideoService _videoService;

    RectItemService _rectItemService;
    #endregion

    public HotificationHub(
        IServiceProvider serviceProvider, 
        ConfigurationService configurationService, 
        VideoService videoService,
        RectItemService rectItemService)
    {
        _serviceProvider = serviceProvider;
        _configurationService = configurationService;
        _videoService = videoService;
        _rectItemService = rectItemService;
    }

    #region Configuration View Model
    public async Task SaveConfig(string сonnectionString, string url, int neuralWatcherTimeout, int frameRate)
    {
        Log.Debug("HotificationHub.SaveConfig: Start.");
        try
        {
            await _configurationService.UpdateAppSettingsAsync(appSettings =>
            {
                appSettings.ConnectionStrings.dbStringConnection = сonnectionString;
                appSettings.ConnectionStrings.srsStringConnection = url;
                appSettings.NeuralWatcherTimeout = neuralWatcherTimeout;
                appSettings.FrameRate.Value = frameRate;
            });

            await Clients.Caller.SendAsync("SaveConfigOk");
            Log.Debug("HotificationHub.SaveConfig: Done.");
            return;
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("SaveConfigFailed");
            return;
        }
    }
    #endregion

    #region Main Window View Model
    public async Task OpenVideoAsync(string fileLocalPath, string videoFileName, string videoFilePath)
    {
        Log.Information("Start sending video");
        Log.Debug("NotificationHub.OpenVideoAsync: Start");
        await Clients.Caller.SendAsync("VideoEventJournalViewModelClear");
        try
        {
            await InitFramesAsync(fileLocalPath, videoFileName, videoFilePath);
            await Clients.Caller.SendAsync("OpenVideoAsyncDoneSuccessfully", true, true);
            return;
        }
        finally
        {
            await Clients.Caller.SendAsync("OpenVideoAsyncFinally", false, 0);
            Log.Information("End sending video");
            Log.Debug("NotificationHub.OpenVideoAsync: Done");
        }
    }
    #endregion

    #region Main Window View Model Private Methods
    private async Task InitFramesAsync(string fileLocalPath, string videoFileName, string videoFilePath)
    {
        Log.Debug("NotificationHub.InitFramesAsync: Start");
        await Clients.Caller.SendAsync("ShowProgressBar");
        var itemsLists = new AvaloniaList<AvaloniaList<RectItem>>();
        var frames = await _videoService.GetFramesAsync(fileLocalPath);

        List<FrameNDetections> frameNDetections = new List<FrameNDetections>();
        int totalFiles = frames.Count();
        for (int idx = 0; idx < totalFiles; idx++)
        {
            var results = await GetFrameDetectionResultsAsync(frames[idx], idx + 1);
            itemsLists.Add(results);
            int progressPercentage = (int)((idx + 1) / (double)totalFiles * 100);
            await Clients.Caller.SendAsync("UpdateProgressPercentage", progressPercentage);
            frameNDetections.Add(new FrameNDetections
            {
                Frame = frames[idx],
                Detections = results
            });
        }

        await SaveDataIntoDatabase(videoFileName, videoFilePath, frameNDetections);
        await Clients.Caller.SendAsync("InitFramesAsyncDoneSuccessfully", itemsLists, frames, videoFileName, 0, fileLocalPath);
        Log.Debug("NotificationHub.InitFramesAsync: End");
    }

    private async Task<AvaloniaList<RectItem>> GetFrameDetectionResultsAsync(Bitmap frame, int numberOfFrame)
    {
        Log.Debug("NotificationHub.GetFrameDetectionResultsAsync: Start");
        List<RecognitionResult> detections = await GetFrameRecognitionResultsAsync(frame, numberOfFrame);
        var items = new AvaloniaList<RectItem>();

        foreach (RecognitionResult det in detections)
        {
            try
            {
                items.Add(_rectItemService.InitRect(det, frame));
                await SaveRecognitionResultAsync(det);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ErrorProcessingDetection", ex);
                Log.Warning("NotificationHub.GetFrameDetectionResultsAsync: Error; Message: {@Message}", ex.Message);
            }
        }
        Log.Debug("MainViewModel.GetFrameDetectionResultsAsync: Done");
        return items;
    }

    private async Task<List<RecognitionResult>> GetFrameRecognitionResultsAsync(Bitmap frame, int numberOfFrame)
    {
        Log.Debug("MainViewModel.GetFrameRecognitionResultsAsync: Start");
        string surfaceRecognitionServiceAddress = _configurationService.GetConnectionString("srsStringConnection");
        using (var client = new HttpClient())
        {
            try
            {
                using (MemoryStream imageStream = new())
                {
                    frame.Save(imageStream);
                    imageStream.Seek(0, SeekOrigin.Begin);

                    var content = new MultipartFormDataContent();
                    var imageContent = new StreamContent(imageStream);
                    imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    content.Add(imageContent, "image", $"frame{numberOfFrame}.img");

                    var response = await client.PostAsync($"{surfaceRecognitionServiceAddress}/inference", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<DetectedAndClassifiedObject>(jsonResponse);

                        if (result?.ObjectBbox != null)
                        {
                            return result.ObjectBbox.Select(bbox => new RecognitionResult
                            {
                                ClassName = bbox.ClassName,
                                X = bbox.X,
                                Y = bbox.Y,
                                Width = bbox.Width,
                                Height = bbox.Height
                            }).ToList();
                        }
                    }
                    else
                    {
                        await Clients.Caller.SendAsync("ErrorWhenSendingVideo", response);
                        Log.Debug("NotificationHub.GetFrameRecognitionResultsAsync: Error; Message: {@Message}", $"Ошибка при отправке видео: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ErrorWhenSendingVideoException", ex);
                Log.Debug("NotificationHub.GetFrameRecognitionResultsAsync: Error; Message: {@Message}", ex.Message);
            }
        }
        Log.Debug("MainViewModel.GetFrameRecognitionResultsAsync: Done");
        return new List<RecognitionResult>();
    }

    private async Task SaveRecognitionResultAsync(RecognitionResult recognitionResult)
    {
        Log.Debug("NotificationHub.SaveRecognitionResultAsync: Start");
        using ApplicationContext db = _serviceProvider.GetRequiredService<ApplicationContext>();
        db.RecognitionResults.AddRange(recognitionResult);
        await db.SaveChangesAsync();
        Log.Debug("NotificationHub.SaveRecognitionResultAsync: Done");
    }
    #endregion

    #region Data Base Methods
    // Переписать в дальнейшем на паттерн Repository
    private async Task<Video> SaveDataIntoDatabase(string videoFileName, string videoFilePath, List<FrameNDetections> framesNDetections)
    {
        Log.Debug("MainViewModel.SaveDataIntoDatabaseAsync: Start");
        using ApplicationContext db = _serviceProvider.GetRequiredService<ApplicationContext>();

        List<Frame> framesModel = new List<Frame>();
        await Task.Run(() =>
        {
            foreach (FrameNDetections currentFrameNDetections in framesNDetections)
            {
                using var memoryStream = new MemoryStream();
                currentFrameNDetections.Frame.Save(memoryStream);
                byte[] frameBytes = memoryStream.ToArray();

                var currentDetections = currentFrameNDetections.Detections.Select((detection) => new Detection
                {
                    ClassName = detection.Color switch
                    {
                        "Green" => "human",
                        "Red" => "wind/sup-board",
                        "Blue" => "bouy",
                        "Yellow" => "sailboat",
                        "Purple" => "kayak"
                    },
                    X = detection.X,
                    Y = detection.Y,
                    Width = detection.Width,
                    Height = detection.Height
                }).ToList();

                Frame currentFrame = new Frame
                {
                    FrameData = frameBytes,
                    CreatedAt = DateTime.UtcNow,
                    Detections = currentDetections
                };
                framesModel.Add(currentFrame);
            }
        });

        Video videoModel = new Video
        {
            VideoName = videoFileName,
            FilePath = videoFilePath,
            CreatedAt = DateTime.UtcNow,
            Frames = framesModel,
        };
        var addedVideoEntity = db.Videos.Add(videoModel);
        await db.SaveChangesAsync();
        return addedVideoEntity.Entity;
    }
    #endregion

    #region Classes
    public class InferenceResult
    {
        [JsonPropertyName("class_name")]
        public string ClassName { get; set; }

        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    public class DetectedAndClassifiedObject
    {
        [JsonPropertyName("object_bbox")]
        public List<InferenceResult> ObjectBbox { get; set; }
    }

    public class FrameNDetections
    {
        public Bitmap Frame { get; set; }

        public AvaloniaList<RectItem> Detections { get; set; }
    }
    #endregion
}
