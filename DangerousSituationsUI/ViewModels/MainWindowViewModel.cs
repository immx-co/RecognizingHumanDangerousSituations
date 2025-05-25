using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ClassLibrary.Database.Models;
using ClassLibrary.Datacontracts;
using ClassLibrary.Repository;
using ClassLibrary.Services;
using DangerousSituationsUI.Services;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using static DangerousSituationsUI.ViewModels.VideoPlayerViewModel;
using System.Collections.ObjectModel;
using SkiaSharp;
using System.Diagnostics;
using FFMpegCore.Enums;


namespace DangerousSituationsUI.ViewModels;

public class MainViewModel : ReactiveObject, IRoutableViewModel
{
    #region Private Fields
    private CancellationTokenSource _rewindCts;

    private bool _neuralPipelineIsLoaded = false;

    private Bitmap? _currentImage;

    private List<Bitmap?> _imageFilesBitmap = new();

    private List<IStorageFile>? _imageFiles = new();

    private IStorageFile? _videoFile;

    private List<Bitmap?> _frames = new();

    private string _currentFileName;

    private string _frameTitle;

    private FilesService _filesService;

    private VideoService _videoService;

    private RectItemService _rectItemService;

    private FigItemService _figItemService;

    private ExportService _exportService;

    private readonly ConfigurationService _configurationService;

    private IServiceProvider _serviceProvider;

    private ISolidColorBrush _connectionStatus;

    private bool _canSwitchImages;

    private bool _isLoading;

    private int _progressPercentage;

    private bool _areButtonsEnabled;

    private bool _rewindBackButtonEnabled;

    private bool _rewindForwardButtonEnabled;

    private bool _rewindPauseButtonEnabled;

    private bool _areConnectButtonEnabled = true;

    private AvaloniaList<RectItem> _rectItems;

    private AvaloniaList<AvaloniaList<RectItem>> _rectItemsLists;

    private AvaloniaList<FigItem> _figItems;

    private AvaloniaList<AvaloniaList<FigItem>> _figItemsLists;

    private AvaloniaList<LegendItem> _legendItems;

    private string _videoTime;

    private TelegramBotAPI _telegramBotApi;

    private int _currentNumberOfFrame;

    private readonly VideoPlayerViewModel _videoPlayerViewModel;

    private AvaloniaList<FrameModel>? _frameItems = new();

    private FrameModel _selectedFrameItem;

    private int count = 0;
    #endregion

    #region Public Fields
    public VideoEventJournalViewModel _videoEventJournalViewModel;
    #endregion

    #region View Model Settings
    public IScreen HostScreen { get; }
    public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

    public CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    #endregion

    #region Commands
    public ReactiveCommand<Unit, Unit> ImageBackCommand { get; }

    public ReactiveCommand<Unit, Unit> ImageForwardCommand { get; }

    public ReactiveCommand<Unit, Unit> ImageRewindBackCommand { get; }

    public ReactiveCommand<Unit, Unit> ImageRewindForwardCommand { get; }

    public ReactiveCommand<Unit, Unit> ImageRewindPauseCommand { get; }

    public ReactiveCommand<Unit, Unit> SendVideoCommand { get; }

    public ReactiveCommand<Unit, Unit> ConnectCommand { get; }

    public ReactiveCommand<Unit, Unit> SendFolderCommand { get; }
    #endregion

    #region Properties
    public AvaloniaList<RectItem> RectItems
    {
        get => _rectItems;
        set => this.RaiseAndSetIfChanged(ref _rectItems, value);
    }

    public AvaloniaList<FigItem> FigItems
    {
        get => _figItems;
        set => this.RaiseAndSetIfChanged(ref _figItems, value);
    }

    public Bitmap? CurrentImage
    {
        get => _currentImage;
        set => this.RaiseAndSetIfChanged(ref _currentImage, value);
    }

    public string CurrentFileName
    {
        get => _currentFileName;
        set => this.RaiseAndSetIfChanged(ref _currentFileName, value);
    }

    public string FrameTitle
    {
        get => _frameTitle;
        set => this.RaiseAndSetIfChanged(ref _frameTitle, value);
    }

    public ISolidColorBrush ConnectionStatus
    {
        get => _connectionStatus;
        private set => this.RaiseAndSetIfChanged(ref _connectionStatus, value);
    }

    public bool CanSwitchImages
    {
        get => _canSwitchImages;
        set => this.RaiseAndSetIfChanged(ref _canSwitchImages, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public int ProgressPercentage
    {
        get => _progressPercentage;
        set => this.RaiseAndSetIfChanged(ref _progressPercentage, value);
    }

    public bool AreButtonsEnabled
    {
        get => _areButtonsEnabled;
        private set => this.RaiseAndSetIfChanged(ref _areButtonsEnabled, value);
    }

    public bool AreConnectButtonEnabled
    {
        get => _areConnectButtonEnabled;
        private set => this.RaiseAndSetIfChanged(ref _areConnectButtonEnabled, value);
    }

    public bool RewindBackButtonEnabled
    {
        get => _rewindBackButtonEnabled;
        private set => this.RaiseAndSetIfChanged(ref _rewindBackButtonEnabled, value);
    }

    public bool RewindForwardButtonEnabled
    {
        get => _rewindForwardButtonEnabled;
        private set => this.RaiseAndSetIfChanged(ref _rewindForwardButtonEnabled, value);
    }

    public bool RewindPauseButtonEnabled
    {
        get => _rewindPauseButtonEnabled;
        private set => this.RaiseAndSetIfChanged(ref _rewindPauseButtonEnabled, value);
    }

    public AvaloniaList<LegendItem> LegendItems
    {
        get => _legendItems;
        set => this.RaiseAndSetIfChanged(ref _legendItems, value);
    }

    public FrameModel SelectedFrameItem
    {
        get => _selectedFrameItem;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedFrameItem, value);

            if (value != null)
            {
                SetFrame(value.id);
            }
        }
    }
    public AvaloniaList<FrameModel>? FrameItems
    {
        get => _frameItems;
        set => this.RaiseAndSetIfChanged(ref _frameItems, value);
    }

    public bool NeuralPipelineIsLoaded { get; set; }
    #endregion

    #region Constructors
    public MainViewModel(
        IScreen screen,
        FilesService filesService,
        ConfigurationService configurationService,
        IServiceProvider serviceProvider,
        VideoService videoService,
        RectItemService rectItemService,
        FigItemService figItemService,
        VideoEventJournalViewModel videoEventJournalViewModel,
        VideoPlayerViewModel videoPlayerViewModel,
        TelegramBotAPI telegramBotApi,
        ExportService exportService)
    {
        HostScreen = screen;
        _filesService = filesService;
        _videoService = videoService;
        _rectItemService = rectItemService;
        _figItemService = figItemService;
        _serviceProvider = serviceProvider;
        _videoEventJournalViewModel = videoEventJournalViewModel;
        _configurationService = configurationService;
        _videoPlayerViewModel = videoPlayerViewModel;
        _telegramBotApi = telegramBotApi;
        _exportService = exportService;

        _telegramBotApi.StartBotAsync();

        AreButtonsEnabled = false;
        RewindBackButtonEnabled = false;
        RewindForwardButtonEnabled = false;
        RewindPauseButtonEnabled = false;

        _legendItems = new AvaloniaList<LegendItem>
        {
            new LegendItem { ClassName = "Standing", Color = "Green" },
            new LegendItem { ClassName = "Lying", Color = "Red" },
        };

        CanSwitchImages = false;

        ConnectCommand = ReactiveCommand.CreateFromTask(CheckHealthAsync);
        SendVideoCommand = ReactiveCommand.CreateFromTask(OpenVideoAsync);
        SendFolderCommand = ReactiveCommand.CreateFromTask(OpenFolderAsync);
        ImageBackCommand = ReactiveCommand.Create(PreviousFrame);
        ImageForwardCommand = ReactiveCommand.Create(NextFrame);
        ImageRewindBackCommand = ReactiveCommand.Create(RewindPreviousFrame);
        ImageRewindForwardCommand = ReactiveCommand.Create(RewindNextFrame);
        ImageRewindPauseCommand = ReactiveCommand.Create(RewindPause);
        _exportService = exportService;
    }
    #endregion

    #region Command Methods
    private async Task OpenFolderAsync()
    {
        Log.Information("Start sending folder");
        Log.Debug("MainViewModel.OpenFolderAsync: Start");

        try
        {
            AreButtonsEnabled = false;
            var files = await _filesService.OpenVideoFolderAsync();

            if (files == null || !files.Any())
            {
                ShowMessageBox("Error", "There are no video files with valid extensions in the folder.");
                Log.Error("Error: Folder don't have videos accessed");
                return;
            }

            FrameItems = new();
            _videoPlayerViewModel.VideoItems = new ObservableCollection<VideoItem>();

            foreach (var file in files)
            {
                try
                {
                    Log.Information($"Start sending video: {file.Name}");
                    CurrentFileName = file.Name;

                    await InitFramesAsync(file);

                    ProgressPercentage = 0;

                    Log.Information($"End sending video: {file.Name}");
                }
                catch (Exception ex)
                {
                    Log.Error($"Error processing video file {file.Name}: {ex.Message}");
                }
            }

            CanSwitchImages = true;
        }
        catch (Exception ex)
        {
            ShowMessageBox("Error", "В выбранной директории отсутствуют изображения или присутствуют файлы с недопустимым расширением.");
            Log.Warning($"MainViewModel.OpenFolderAsync: Error; Message: {ex.Message}");
        }
        finally
        {
            AreButtonsEnabled = true;
            IsLoading = false;
            ProgressPercentage = 0;
            Log.Information($"End sending Folder");
            Log.Debug("MainViewModel.OpenFolderAsync: Done");
        }
    }


    private async Task OpenVideoAsync()
    {
        Log.Information("Start sending video");
        Log.Debug("MainViewModel.OpenVideoAsync: Start");
        try
        {
            AreButtonsEnabled = false;
            var file = await _filesService.OpenVideoFileAsync();
            if (file != null)
            {
                FrameItems = new();
                _videoPlayerViewModel.VideoItems = new ObservableCollection<VideoItem>();
                await InitFramesAsync(file);
                CanSwitchImages = true;
                    FrameTitle = $"{_currentNumberOfFrame + 1} / {_frames.Count}";
                }
            }
        finally
        {
            AreButtonsEnabled = true;
            IsLoading = false;
            ProgressPercentage = 0;
            Log.Information("End sending video");
            Log.Debug("MainViewModel.OpenVideoAsync: Done");
        }
    }
    #endregion

    private void ResetUI()
    {
        RectItems = new AvaloniaList<RectItem>();
        FigItems = new AvaloniaList<FigItem>();
        CurrentImage = null;
        CurrentFileName = String.Empty;
        FrameTitle = String.Empty;
    }

    #region Video Methods
    private async Task InitializeVideoEventJournalWindow()
    {
        await _videoEventJournalViewModel.FillComboBox();
    }

    private async Task InitFramesAsync(IStorageFile file)
    {
        Log.Debug($"MainViewModel.InitFramesAsync: Start: {file.Name}");
        IsLoading = true;
        var itemsLists = new AvaloniaList<AvaloniaList<RectItem>>();
        var figLists = new AvaloniaList<AvaloniaList<FigItem>>();
        List<FrameNDetections> frameNDetections = new List<FrameNDetections>();
        List<BitmapModel> frameBitmapModels = null;

        var results = new AvaloniaList<RectItem>();
        var figResults = new AvaloniaList<FigItem>();

        string jsonPath = Path.ChangeExtension(file.Path.LocalPath, ".json");

        if (File.Exists(jsonPath))
        {
            try
            {
                Log.Debug($"Loading from JSON file: {file.Name}");

                var json = await File.ReadAllTextAsync(jsonPath);
                if (string.IsNullOrWhiteSpace(json))
                    throw new Exception("JSON file is empty.");

                var videoExportItem = JsonSerializer.Deserialize<VideoExportItem>(json);
                if (videoExportItem == null)
                    throw new Exception("Deserialized VideoExportItem is null.");

                var jsonItems = videoExportItem.Items ?? throw new Exception("No detection items found in JSON.");

                frameBitmapModels = await _videoService.GetFramesFromClipAndJsonAsync(file.Path.LocalPath, videoExportItem);

                foreach (var item in jsonItems)
                {
                    var matchingFrame = frameBitmapModels.FirstOrDefault(b => b.timeSpan == item.FrameTime);
                    if (matchingFrame == null) continue;

                    itemsLists.Add(item.RectItems);
                    figLists.Add(item.FigItems);

                    frameNDetections.Add(new FrameNDetections
                    {
                        Frame = matchingFrame,
                        Detections = item.RectItems,
                        Figs = item.FigItems
                    });
                }

                FrameItems?.Add(new FrameModel
                {
                    Name = file.Name,
                    frames = frameBitmapModels,
                    rectitems = itemsLists,
                    figitems = figLists,
                    id = count++
                });

            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error loading JSON detections: {file.Name}");
                ShowMessageBox("Error", $"Ошибка загрузки из JSON.\n{ex.Message}");
            }
        }

        else
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            frameBitmapModels = await _videoService.GetFramesAsync(file);
            int totalFiles = frameBitmapModels.Count();
            for (int idx = 0; idx < totalFiles; idx++)
            {
                (results, figResults) = await GetFrameDetectionResultsAsync(frameBitmapModels[idx], idx + 1);
                itemsLists.Add(results);
                figLists.Add(figResults);
                ProgressPercentage = (int)((idx + 1) / (double)totalFiles * 100);
                frameNDetections.Add(new FrameNDetections
                {
                    Frame = frameBitmapModels[idx],
                    Detections = results,
                    Figs = figResults
                });
            }

            FrameItems?.Add(new FrameModel
            {
                Name = file.Name,
                frames = frameBitmapModels,
                rectitems = itemsLists,
                figitems = figLists,
                id = count++
            });

            stopwatch.Stop();
            ShowMessageBox("Inference Time", $"Время обработки файла видео: {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)}\n" +
                $"FPS: {Math.Round(totalFiles / stopwatch.Elapsed.TotalSeconds, 2)}");
        }

        _ = Task.Run(async () =>
        {
            await SaveDataIntoDatabase(file, frameNDetections);
        });

        if (frameBitmapModels != null)
        {
            _frames = frameBitmapModels.Select(bm => bm.frame).ToList();
        }
        else
        {
            Log.Error($"No frame bitmaps were loaded: {file.Name}");
            return;
        }

        _videoFile = file;
        _rectItemsLists = itemsLists;
        _figItemsLists = figLists;
        CurrentFileName = file.Name;

        RewindBackButtonEnabled = true;
        RewindForwardButtonEnabled = true;

        if (FrameItems != null && FrameItems.Count > 0)
        {
            SelectedFrameItem = FrameItems[0];
            _currentNumberOfFrame = 0;
        }
        else
        {
            Log.Error($"No frames were loaded: {file.Name}");
        }
        Log.Debug($"MainViewModel.InitFramesAsync: End: {file.Name}");

    }

    private async Task<Video> SaveDataIntoDatabase(IStorageFile videoFile, List<FrameNDetections> framesNDetections)
    {
        
        Log.Debug($"MainViewModel.SaveDataIntoDatabaseAsync: Start: {videoFile.Name}");

        _videoPlayerViewModel.IsVideoLoading = true;
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository>();

        var videoModel = new Video
        {
            VideoName = videoFile.Name,
            FilePath = videoFile.Path.ToString(),
            CreatedAt = DateTime.UtcNow,
            Frames = new List<Frame>()
        };

        foreach (var frameNDetection in framesNDetections)
        {
            using var memoryStream = new MemoryStream();
            frameNDetection.Frame.frame.Save(memoryStream);
            byte[] frameBytes = memoryStream.ToArray();

            var detections = frameNDetection.Detections.Select((rect, i) => 
            {
                var fig = frameNDetection.Figs[i];

                return new Detection
                {
                    ClassName = rect.Color switch
                    {
                        "Green" => "Standing",
                        "Red" => "Lying"
                    },
                    X = rect.X,
                    Y = rect.Y,
                    Width = rect.Width,
                    Height = rect.Height,
                    Nose = fig.Nose,
                    LeftEye = fig.LeftEye,
                    RightEye = fig.RightEye,
                    LeftEar = fig.LeftEar,
                    RightEar = fig.RightEar,
                    LeftShoulder = fig.LeftShoulder,
                    RightShoulder = fig.RightShoulder,
                    LeftElbow = fig.LeftElbow,
                    RightElbow = fig.RightElbow,
                    LeftWrist = fig.LeftWrist,
                    RightWrist = fig.RightWrist,
                    LeftHip = fig.LeftHip,
                    RightHip = fig.RightHip,
                    LeftKnee = fig.LeftKnee,
                    RightKnee = fig.RightKnee,
                    LeftAnkle = fig.LeftAnkle,
                    RightAnkle = fig.RightAnkle
                };
            })
            .ToList();

            var frame = new Frame
            {
                FrameData = frameBytes,
                CreatedAt = DateTime.UtcNow,
                Detections = detections,
                FrameTime = frameNDetection.Frame.timeSpan
            };

            videoModel.Frames.Add(frame);
        }

        var addedVideo = await repository.AddVideoAsync(videoModel);
        await repository.SaveChangesAsync();

        _videoPlayerViewModel.VideoItems.Add(new VideoItem
        {
            Guid = videoModel.VideoId,
            Name = videoModel.VideoName,
            Path = videoFile.Path.LocalPath
        });
        _videoPlayerViewModel.SelectedVideoItem = _videoPlayerViewModel.VideoItems.First();

        await _videoEventJournalViewModel.FillComboBox();
        _videoPlayerViewModel.IsVideoLoading = false;

        Log.Debug($"MainViewModel.SaveDataIntoDatabaseAsync: Done: {videoFile.Name}");
        return addedVideo;
    }

    private async Task<(AvaloniaList<RectItem> Rects, AvaloniaList<FigItem> Figs)> GetFrameDetectionResultsAsync(BitmapModel frameBitmapModel, int numberOfFrame)
    {
        Log.Debug($"MainViewModel.GetFrameDetectionResultsAsync: Start: TimeOfFrame:{frameBitmapModel.timeSpan}");
        List<RecognitionResult> detections = await GetFrameRecognitionResultsAsync(frameBitmapModel.frame, numberOfFrame);
        var items = new AvaloniaList<RectItem>();
        var figItems = new AvaloniaList<FigItem>();

        foreach (RecognitionResult det in detections)
        {
            try
            {
                var rectItem = _rectItemService.InitRect(det, frameBitmapModel.frame);
                items.Add(rectItem);

                var figItem = _figItemService.InitFig(det, frameBitmapModel.frame.Size);
                figItems.Add(figItem);

                if (det.ClassName == "Lying")
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _telegramBotApi.SendEventDataWrapperAsync(frameBitmapModel, det);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("Telegram send error: {Error}", ex.Message);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox("Error", $"Ошибка при обработке детекции: {ex.Message}");
            }
        }
        Log.Debug("MainViewModel.GetFrameDetectionResultsAsync: Done");
        return (items, figItems);
    }

    private async Task<List<RecognitionResult>> GetFrameRecognitionResultsAsync(Bitmap frame, int numberOfFrame)
    {
        Log.Debug($"MainViewModel.GetFrameRecognitionResultsAsync: Start");
        string surfaceRecognitionServiceAddress = _configurationService.GetConnectionString("srsStringConnection");
        using (var client = new HttpClient())
        {
            if (!NeuralPipelineIsLoaded)
            {
                var parameters = new Dictionary<string, string>
                {
                    { "confidence", "0.25" }
                };
                var contentLoadModel = new FormUrlEncodedContent(parameters);
                var responseModelLoaded = await client.PostAsync($"{surfaceRecognitionServiceAddress}/load_model", contentLoadModel);
                if (responseModelLoaded.IsSuccessStatusCode)
                {
                    NeuralPipelineIsLoaded = true;
                }
                Log.Information($"Neural models is loaded. responseModelLoaded status code: {responseModelLoaded.StatusCode}");
            }

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

                    var response = await client.PostAsync($"{surfaceRecognitionServiceAddress}/image_inference", content);

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
                                Height = bbox.Height,
                                Nose = bbox.Keypoints.Nose,
                                LeftEye = bbox.Keypoints.LeftEye,
                                RightEye = bbox.Keypoints.RightEye,
                                LeftEar = bbox.Keypoints.LeftEar,
                                RightEar = bbox.Keypoints.RightEar,
                                LeftShoulder = bbox.Keypoints.LeftShoulder,
                                RightShoulder = bbox.Keypoints.RightShoulder,
                                LeftElbow = bbox.Keypoints.LeftElbow,
                                RightElbow = bbox.Keypoints.RightElbow,
                                LeftWrist = bbox.Keypoints.LeftWrist,
                                RightWrist = bbox.Keypoints.RightWrist,
                                LeftHip = bbox.Keypoints.LeftHip,
                                RightHip = bbox.Keypoints.RightHip,
                                LeftKnee = bbox.Keypoints.LeftKnee,
                                RightKnee = bbox.Keypoints.RightKnee,
                                LeftAnkle = bbox.Keypoints.LeftAnkle,
                                RightAnkle = bbox.Keypoints.RightAnkle,
                            }).ToList();
                        }
                    }
                    else
                    {
                        ShowMessageBox("Error", $"Ошибка при отправке видео: {response.StatusCode}");
                        Log.Debug("MainViewModel.GetFrameRecognitionResultsAsync: Error; Message: {@Message}", $"Ошибка при отправке видео: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox("Error", $"Ошибка при отправке видео: {ex.Message}");
                Log.Debug("MainViewModel.GetFrameRecognitionResultsAsync: Error; Message: {@Message}", ex.Message);
            }
        }
        Log.Debug("MainViewModel.GetFrameRecognitionResultsAsync: Done");
        return new List<RecognitionResult>();
    }

    private void SetFrame()
    {
        CurrentImage = _frames[_currentNumberOfFrame];
        RectItems = _rectItemsLists[_currentNumberOfFrame];
        FigItems = _figItemsLists[_currentNumberOfFrame];

        FrameTitle = $"{_currentNumberOfFrame + 1} / {_frames.Count}";
    }

    private void SetFrame(int id)
    {
        foreach (var Frames in FrameItems)
        {
            if (Frames.id == id)
            {

                _frames = Frames.frames.Select(bm => bm.frame).ToList();
                _rectItemsLists = Frames.rectitems;
                _figItemsLists = Frames.figitems;
                CurrentFileName = Frames.Name;

            }
        }
        _currentNumberOfFrame = 0;

        CurrentImage = _frames[_currentNumberOfFrame];
        RectItems = _rectItemsLists[_currentNumberOfFrame];
        FigItems = _figItemsLists[_currentNumberOfFrame];
        FrameTitle = $"{_currentNumberOfFrame + 1} / {_frames.Count}";
    }
    #endregion

    #region Scroll Methods
    private void NextFrame()
    {
        if (_currentNumberOfFrame < _frames.Count - 1) _currentNumberOfFrame++;
        else _currentNumberOfFrame = 0;

        SetFrame();
    }

    private void PreviousFrame()
    {
        if (_currentNumberOfFrame > 0) _currentNumberOfFrame--;
        else _currentNumberOfFrame = _frames.Count - 1;

        SetFrame();
    }

    private async void RewindPreviousFrame()
    {
        try
        {
            StopRewind();

            CanSwitchImages = false;
            RewindBackButtonEnabled = false;
            RewindForwardButtonEnabled = true;
            RewindPauseButtonEnabled = true;

            _rewindCts = new CancellationTokenSource();
            await RewindFramesAsync(forward: false, _rewindCts.Token);
        }
        catch (OperationCanceledException)
        {
            ;
        }
    }

    private async void RewindNextFrame()
    {
        try
        {
            StopRewind();

            CanSwitchImages = false;
            RewindForwardButtonEnabled = false;
            RewindBackButtonEnabled = true;
            RewindPauseButtonEnabled = true;

            _rewindCts = new CancellationTokenSource();
            await RewindFramesAsync(forward: true, _rewindCts.Token);
        }
        catch (OperationCanceledException)
        {
            ;
        }
    }

    private void RewindPause()
    {
        StopRewind();
        CanSwitchImages = true;
        RewindBackButtonEnabled = true;
        RewindForwardButtonEnabled = true;
    }

    private async Task RewindFramesAsync(bool forward, CancellationToken cancellationToken)
    {
        int frameScrollTimeout = _configurationService.GetFrameScrollTimeout();

        while (!cancellationToken.IsCancellationRequested)
        {
            if (forward) NextFrame();
            else PreviousFrame();
            await Task.Delay(frameScrollTimeout);
        }
    }

    private void StopRewind()
    {
        _rewindCts?.Cancel();
        _rewindCts?.Dispose();
        _rewindCts = null;
    }
    #endregion

    #region Client Methods
    private async Task CheckHealthAsync()
    {
        _serviceProvider.GetRequiredService<NavigationViewModel>().ConnectionStatus = Brushes.Red;
        string surfaceRecognitionServiceAddress = _configurationService.GetConnectionString("srsStringConnection");
        using (var client = new HttpClient())
        {
            Log.Debug("MainViewModel.CheckHealthAsync: Start");
            try
            {
                var response = await client.GetAsync($"{surfaceRecognitionServiceAddress}/health");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var healthResponse = JsonSerializer.Deserialize<HealthCheckResponse>(jsonResponse);
                    if (healthResponse?.StatusCode == 200)
                    {
                        _serviceProvider.GetRequiredService<NavigationViewModel>().ConnectionStatus = Brushes.Green;
                        AreButtonsEnabled = true;
                        AreConnectButtonEnabled = false;
                        ShowMessageBox("Success", $"Сервис доступен. Статус: {healthResponse.StatusCode}");
                        Task.Run(() => StartNeuralServiceWatcher());
                        Task.Run(InitializeVideoEventJournalWindow);
                        Log.Debug("MainViewModel.CheckHealthAsync: Health checked");
                    }
                    else
                    {
                        _serviceProvider.GetRequiredService<NavigationViewModel>().ConnectionStatus = Brushes.Red;
                        _videoEventJournalViewModel.ClearUI();
                        ShowMessageBox("Failed", $"Сервис недоступен. Статус: {healthResponse?.StatusCode}");
                        Log.Warning("MainViewModel.CheckHealthAsync: Error; Message: {@Message}", $"Сервис недоступен. Статус: {healthResponse?.StatusCode}");
                    }
                }
                else
                {
                    _serviceProvider.GetRequiredService<NavigationViewModel>().ConnectionStatus = Brushes.Red;
                    _videoEventJournalViewModel.ClearUI();
                    ShowMessageBox("Failed", $"Не удалось подключиться к сервису с адресом {surfaceRecognitionServiceAddress}");
                    Log.Warning("MainViewModel.CheckHealthAsync: Error; Message: {@Message}", $"Не удалось подключиться к сервису с адресом {surfaceRecognitionServiceAddress}");
                }
            }
            catch
            {
                _serviceProvider.GetRequiredService<NavigationViewModel>().ConnectionStatus = Brushes.Red;
                _videoEventJournalViewModel.ClearUI();
                ShowMessageBox("Failed", $"Не удалось подключиться к сервису с адресом {surfaceRecognitionServiceAddress}");
                Log.Warning("MainViewModel.CheckHealthAsync: Error; Message: {@Message}", $"Не удалось подключиться к сервису с адресом {surfaceRecognitionServiceAddress}");
            }
        }
    }

    private async void StartNeuralServiceWatcher()
    {
        Log.Debug("MainViewModel.StartNeuralServiceWatcher: Start health check");
        while (true)
        {
            string surfaceRecognitionServiceAddress = _configurationService.GetConnectionString("srsStringConnection");
            int neuralWatcherTimeout = _configurationService.GetNeuralWatcherTimeout();
            using (var client = new HttpClient())
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(neuralWatcherTimeout));
                    var response = await client.GetAsync($"{surfaceRecognitionServiceAddress}/health");
                    if (response.IsSuccessStatusCode)
                    {
                        continue;
                    }
                    else
                    {
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            ResetUI();
                            AreConnectButtonEnabled = true;
                            AreButtonsEnabled = false;
                            _serviceProvider.GetRequiredService<NavigationViewModel>().ConnectionStatus = Brushes.Red;
                            _videoEventJournalViewModel.ClearUI();
                            ShowMessageBox("Failed", "Пропало соединение с нейросетевым сервисом, попробуйте подключиться еще раз.");
                            Log.Debug("MainViewModel.StartNeuralServiceWatcher: Error; Message: {@Message}", "Пропало соединение с нейросетевым сервисом, попробуйте подключиться еще раз.");
                        });
                        break;
                    }
                }
                catch (Exception)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ResetUI();
                        AreConnectButtonEnabled = true;
                        AreButtonsEnabled = false;
                        _serviceProvider.GetRequiredService<NavigationViewModel>().ConnectionStatus = Brushes.Red;
                        _videoEventJournalViewModel.ClearUI();
                        ShowMessageBox("Failed", "Пропало соединение с нейросетевым сервисом, попробуйте подключиться еще раз.");
                        Log.Debug("MainViewModel.StartNeuralServiceWatcher: Error; Message: {@Message}", "Пропало соединение с нейросетевым сервисом, попробуйте подключиться еще раз.");
                    });
                    break;
                }
            }
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Показывает всплывающее сообщение.
    /// </summary>
    /// <param name="caption">Заголовок сообщения.</param>
    /// <param name="message">Сообщение пользователю.</param>
    public void ShowMessageBox(string caption, string message)
    {
        var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandard(caption, message);
        messageBoxStandardWindow.ShowWindowDialogAsync(App.Current.CurrentWindow);
    }

    public void ClearUI()
    {
        ResetUI();
        _telegramBotApi.ChatId = null;
        AreButtonsEnabled = false;
        CanSwitchImages = false;
        _frameItems = new();
        _currentImage = null;
        _frames = new();
        AreConnectButtonEnabled = true;
        AreButtonsEnabled = false;
        _serviceProvider.GetRequiredService<NavigationViewModel>().ConnectionStatus = Brushes.Red;
        _serviceProvider.GetRequiredService<NavigationViewModel>().TgBotConnectionStatus = Brushes.Red;
    }
    #endregion

    #region Classes
    private class HealthCheckResponse
    {
        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("datetime")]
        public DateTime Datetime { get; set; }
    }

    public class KeypointsYoloModels
    {
        /// <summary>Координаты носа</summary>
        [JsonPropertyName("nose")]
        public List<float> Nose { get; set; }

        /// <summary>Координаты левого глаза</summary>
        [JsonPropertyName("left_eye")]
        public List<float> LeftEye { get; set; }

        /// <summary>Координаты правого глаза</summary>
        [JsonPropertyName("right_eye")]
        public List<float> RightEye { get; set; }

        /// <summary>Координаты левого уха</summary>
        [JsonPropertyName("left_ear")]
        public List<float> LeftEar { get; set; }

        /// <summary>Координаты правого уха</summary>
        [JsonPropertyName("right_ear")]
        public List<float> RightEar { get; set; }

        /// <summary>Координаты левого плеча</summary>
        [JsonPropertyName("left_shoulder")]
        public List<float> LeftShoulder { get; set; }

        /// <summary>Координаты правого плеча</summary>
        [JsonPropertyName("right_shoulder")]
        public List<float> RightShoulder { get; set; }

        /// <summary>Координаты левого локтя</summary>
        [JsonPropertyName("left_elbow")]
        public List<float> LeftElbow { get; set; }

        /// <summary>Координаты правого локтя</summary>
        [JsonPropertyName("right_elbow")]
        public List<float> RightElbow { get; set; }

        /// <summary>Координаты левого запястья</summary>
        [JsonPropertyName("left_wrist")]
        public List<float> LeftWrist { get; set; }

        /// <summary>Координаты правого запястья</summary>
        [JsonPropertyName("right_wrist")]
        public List<float> RightWrist { get; set; }

        /// <summary>Координаты левого бедра</summary>
        [JsonPropertyName("left_hip")]
        public List<float> LeftHip { get; set; }

        /// <summary>Координаты правого бедра</summary>
        [JsonPropertyName("right_hip")]
        public List<float> RightHip { get; set; }

        /// <summary>Координаты левого колена</summary>
        [JsonPropertyName("left_knee")]
        public List<float> LeftKnee { get; set; }

        /// <summary>Координаты правого колена</summary>
        [JsonPropertyName("right_knee")]
        public List<float> RightKnee { get; set; }

        /// <summary>Координаты левой лодыжки</summary>
        [JsonPropertyName("left_ankle")]
        public List<float> LeftAnkle { get; set; }

        /// <summary>Координаты правой лодыжки</summary>
        [JsonPropertyName("right_ankle")]
        public List<float> RightAnkle { get; set; }
    }

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

        [JsonPropertyName("keypoints")]
        public KeypointsYoloModels Keypoints { get; set; }
    }

    public class DetectedAndClassifiedObject
    {
        [JsonPropertyName("object_bbox")]
        public List<InferenceResult> ObjectBbox { get; set; }
    }

    public class FrameModel
    {
        public int id { get; set; }

        public List<BitmapModel> frames { get; set; }

        public AvaloniaList<AvaloniaList<RectItem>> rectitems { get; set; } = new();

        public AvaloniaList<AvaloniaList<FigItem>> figitems { get; set; } = new();

        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    

    #endregion
}
