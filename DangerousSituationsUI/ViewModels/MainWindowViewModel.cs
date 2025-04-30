using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ClassLibrary.Database;
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


namespace DangerousSituationsUI.ViewModels;

public class MainViewModel : ReactiveObject, IRoutableViewModel
{
    #region Private Fields
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
    private readonly ConfigurationService _configurationService;

    private IServiceProvider _serviceProvider;

    private ISolidColorBrush _connectionStatus;

    private bool _canSwitchImages;

    private bool _isLoading;

    private int _progressPercentage;

    private bool _areButtonsEnabled;

    private bool _areConnectButtonEnabled = true;

    private AvaloniaList<RectItem> _rectItems;

    private AvaloniaList<AvaloniaList<RectItem>> _rectItemsLists;

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
        VideoEventJournalViewModel videoEventJournalViewModel,
        VideoPlayerViewModel videoPlayerViewModel,
        TelegramBotAPI telegramBotApi)
    {
        HostScreen = screen;
        _filesService = filesService;
        _videoService = videoService;
        _rectItemService = rectItemService;
        _serviceProvider = serviceProvider;
        _videoEventJournalViewModel = videoEventJournalViewModel;
        _configurationService = configurationService;
        _videoPlayerViewModel = videoPlayerViewModel;
        _telegramBotApi = telegramBotApi;

        _telegramBotApi.StartBotAsync();

        AreButtonsEnabled = false;

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
    }
    #endregion

    #region Command Methods
    private async Task OpenFolderAsync()
    {
        Log.Information("Start sending folder");
        LogJournalViewModel.logString += "Start sending folder\n";
        Log.Debug("MainViewModel.OpenFolderAsync: Start");
        LogJournalViewModel.logString += "MainViewModel.OpenFolderAsync: Start\n";
        try
        {
            AreButtonsEnabled = false;
            var files = await _filesService.OpenVideoFolderAsync();
            if (files != null)
            {
                FrameItems = new();
                _videoPlayerViewModel.VideoItems = new ObservableCollection<VideoItem>();

                foreach (var file in files)
                {
                    Log.Information("Start sending video");
                    LogJournalViewModel.logString += "Start sending video\n";
                    CurrentFileName = file.Name;
                    _videoPlayerViewModel.VideoItems.Add(new VideoItem
                    {
                        Name = file.Name,
                        Path = file.Path.LocalPath
                    });
                    await InitFramesAsync(file);
                    ProgressPercentage = 0;
                    Log.Information("End sending video");
                    LogJournalViewModel.logString += "End sending video\n";
                }
                CanSwitchImages = true;
            }
        }
        catch
        {
            ShowMessageBox("Error", "В выбранной директории отсутcтвуют изображения или пристуствуют файлы с недопустимым расширением.");
            Log.Warning("MainViewModel.OpenFolderAsync: Error; Message: В выбранной директории отсутcnвуют изображения или пристуствуют файлы с недопустимым расширением.");
            LogJournalViewModel.logString += "MainViewModel.OpenFolderAsync: Error; Message: В выбранной директории отсутcnвуют изображения или пристуствуют файлы с недопустимым расширением.\n";
            return;
        }
        finally
        {
            AreButtonsEnabled = true;
            IsLoading = false;
            ProgressPercentage = 0;
            await _videoEventJournalViewModel.FillComboBox();
            Log.Information("End sending video");
            LogJournalViewModel.logString += "End sending video\n";
            Log.Debug("MainViewModel.OpenFolderAsync: Done");
            LogJournalViewModel.logString += "MainViewModel.OpenFolderAsync: Done\n";
        }
    }
    private async Task OpenVideoAsync()
    {
        Log.Information("Start sending video");
        LogJournalViewModel.logString += "Start sending video\n";
        Log.Debug("MainViewModel.OpenVideoAsync: Start");
        LogJournalViewModel.logString += "MainViewModel.OpenVideoAsync: Start\n";
        _videoEventJournalViewModel.EventResults = new AvaloniaList<string>();
        try
        {
            AreButtonsEnabled = false;
            var file = await _filesService.OpenVideoFileAsync();
            if (file != null)
            {
                FrameItems = new();
                await InitFramesAsync(file);
                CanSwitchImages = true;
                FrameTitle = $"{_currentNumberOfFrame + 1} / {_frames.Count}";

                _videoPlayerViewModel.VideoItems = new ObservableCollection<VideoItem>
                {
                    new VideoItem
                    {
                        Name = file.Name,
                        Path = file.Path.LocalPath
                    }
                };
                _videoPlayerViewModel.SelectedVideoItem = _videoPlayerViewModel.VideoItems.First();
            }
        }
        finally
        {
            AreButtonsEnabled = true;
            IsLoading = false;
            ProgressPercentage = 0;
            await _videoEventJournalViewModel.FillComboBox();
            Log.Information("End sending video");
            LogJournalViewModel.logString += "End sending video\n";
            Log.Debug("MainViewModel.OpenVideoAsync: Done");
            LogJournalViewModel.logString += "MainViewModel.OpenVideoAsync: Done\n";
        }
    }
    #endregion

    private void ResetUI()
    {
        RectItems = new AvaloniaList<RectItem>();
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
        Log.Debug("MainViewModel.InitFramesAsync: Start");
        LogJournalViewModel.logString += "MainViewModel.InitFramesAsync: Start\n";
        IsLoading = true;
        var itemsLists = new AvaloniaList<AvaloniaList<RectItem>>();
        var frames = await _videoService.GetFramesAsync(file);
        var results = new AvaloniaList<RectItem>();
        List<FrameNDetections> frameNDetections = new List<FrameNDetections>();
        int totalFiles = frames.Count();
        for (int idx = 0; idx < totalFiles; idx++)
        {
            results = await GetFrameDetectionResultsAsync(frames[idx], idx + 1);
            itemsLists.Add(results);
            ProgressPercentage = (int)((idx + 1) / (double)totalFiles * 100);
            frameNDetections.Add(new FrameNDetections
            {
                Frame = frames[idx],
                Detections = results
            });
        }

        FrameItems?.Add(new FrameModel
        {
            Name = file.Name,
            frames = frames,
            rectitems = itemsLists,
            id = count++
        });

        await SaveDataIntoDatabase(file, frameNDetections);

        _videoFile = file;
        _rectItemsLists = itemsLists;
        _frames = frames;

        CurrentFileName = file.Name;
        _currentNumberOfFrame = 0;
        SelectedFrameItem = FrameItems[0];
        Log.Debug("MainViewModel.InitFramesAsync: End");
        LogJournalViewModel.logString += "MainViewModel.InitFramesAsync: End\n";
    }

    private async Task<Video> SaveDataIntoDatabase(IStorageFile videoFile, List<FrameNDetections> framesNDetections)
    {
        Log.Debug("MainViewModel.SaveDataIntoDatabaseAsync: Start");
        LogJournalViewModel.logString += "MainViewModel.SaveDataIntoDatabaseAsync: Start\n";

        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository>();

        // Создаем модель Video
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
            frameNDetection.Frame.Save(memoryStream);
            byte[] frameBytes = memoryStream.ToArray();

            var detections = frameNDetection.Detections.Select(detection => new Detection
            {
                ClassName = detection.Color switch
                {
                    "Green" => "Standing",
                    "Red" => "Lying",
                },
                X = detection.X,
                Y = detection.Y,
                Width = detection.Width,
                Height = detection.Height
            }).ToList();

            var frame = new Frame
            {
                FrameData = frameBytes,
                CreatedAt = DateTime.UtcNow,
                Detections = detections
            };

            videoModel.Frames.Add(frame);

            if (detections.Any())
            {
                var detectionInfo = string.Join("\n", detections.Select(det =>
                    $"{det.ClassName} обнаружен! Координаты: X={det.X}, Y={det.Y}, Ширина={det.Width}, Высота={det.Height}"));
                await _telegramBotApi.SendEventData(frameBytes, detectionInfo);
            }
        }

        var addedVideo = await repository.AddVideoAsync(videoModel);
        await repository.SaveChangesAsync();

        Log.Debug("MainViewModel.SaveDataIntoDatabaseAsync: Done");
        LogJournalViewModel.logString += "MainViewModel.SaveDataIntoDatabaseAsync: Done\n";
        return addedVideo;
    }

    private async Task<AvaloniaList<RectItem>> GetFrameDetectionResultsAsync(Bitmap frame, int numberOfFrame)
    {
        Log.Debug("MainViewModel.GetFrameDetectionResultsAsync: Start");
        LogJournalViewModel.logString += "MainViewModel.GetFrameDetectionResultsAsync: Start\n";
        List<RecognitionResult> detections = await GetFrameRecognitionResultsAsync(frame, numberOfFrame);
        var items = new AvaloniaList<RectItem>();

        foreach (RecognitionResult det in detections)
        {
            try
            {
                items.Add(_rectItemService.InitRect(det, frame));
            }
            catch (Exception ex)
            {
                ShowMessageBox("Error", $"Ошибка при обработке детекции: {ex.Message}");
                Log.Warning("MainViewModel.GetFrameDetectionResultsAsync: Error; Message: {@Message}", ex.Message);
                LogJournalViewModel.logString += ("MainViewModel.GetFrameDetectionResultsAsync: Error; Message: {@Message}\n", ex.Message);
            }
        }
        Log.Debug("MainViewModel.GetFrameDetectionResultsAsync: Done");
        LogJournalViewModel.logString += "MainViewModel.GetFrameDetectionResultsAsync: Done\n";
        return items;
    }

    private async Task<List<RecognitionResult>> GetFrameRecognitionResultsAsync(Bitmap frame, int numberOfFrame)
    {
        Log.Debug("MainViewModel.GetFrameRecognitionResultsAsync: Start");
        LogJournalViewModel.logString += "MainViewModel.GetFrameRecognitionResultsAsync: Start\n";
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
                LogJournalViewModel.logString += $"Neural models is loaded. responseModelLoaded status code: {responseModelLoaded.StatusCode}\n";
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
                                Height = bbox.Height
                            }).ToList();
                        }
                    }
                    else
                    {
                        ShowMessageBox("Error", $"Ошибка при отправке видео: {response.StatusCode}");
                        Log.Debug("MainViewModel.GetFrameRecognitionResultsAsync: Error; Message: {@Message}", $"Ошибка при отправке видео: {response.StatusCode}");
                        LogJournalViewModel.logString += ("MainViewModel.GetFrameRecognitionResultsAsync: Error; Message: {@Message}", $"Ошибка при отправке видео: {response.StatusCode}\n");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox("Error", $"Ошибка при отправке видео: {ex.Message}");
                Log.Debug("MainViewModel.GetFrameRecognitionResultsAsync: Error; Message: {@Message}", ex.Message);
                LogJournalViewModel.logString += ("MainViewModel.GetFrameRecognitionResultsAsync: Error; Message: {@Message}\n", ex.Message);
            }
        }
        Log.Debug("MainViewModel.GetFrameRecognitionResultsAsync: Done");
        LogJournalViewModel.logString += "MainViewModel.GetFrameRecognitionResultsAsync: Done\n";
        return new List<RecognitionResult>();
    }

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

    private void SetFrame()
    {
        CurrentImage = _frames[_currentNumberOfFrame];
        RectItems = _rectItemsLists[_currentNumberOfFrame];

        FrameTitle = $"{_currentNumberOfFrame + 1} / {_frames.Count}";
    }

    private void SetFrame(int id)
    {
        foreach (var Frames in FrameItems)
        {
            if (Frames.id == id)
            {

                _frames = Frames.frames;
                _rectItemsLists = Frames.rectitems;
                CurrentFileName = Frames.Name;

            }
        }
        _currentNumberOfFrame = 0;

        CurrentImage = _frames[_currentNumberOfFrame];
        RectItems = _rectItemsLists[_currentNumberOfFrame];
        FrameTitle = $"{_currentNumberOfFrame + 1} / {_frames.Count}";
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
            LogJournalViewModel.logString += "MainViewModel.CheckHealthAsync: Start\n";
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
                        LogJournalViewModel.logString += "MainViewModel.CheckHealthAsync: Health checked\n";
                    }
                    else
                    {
                        _serviceProvider.GetRequiredService<NavigationViewModel>().ConnectionStatus = Brushes.Red;
                        _videoEventJournalViewModel.ClearUI();
                        ShowMessageBox("Failed", $"Сервис недоступен. Статус: {healthResponse?.StatusCode}");
                        Log.Warning("MainViewModel.CheckHealthAsync: Error; Message: {@Message}", $"Сервис недоступен. Статус: {healthResponse?.StatusCode}");
                        LogJournalViewModel.logString += ("MainViewModel.CheckHealthAsync: Error; Message: {@Message}", $"Сервис недоступен. Статус: {healthResponse?.StatusCode}\n");
                    }
                }
                else
                {
                    _serviceProvider.GetRequiredService<NavigationViewModel>().ConnectionStatus = Brushes.Red;
                    _videoEventJournalViewModel.ClearUI();
                    ShowMessageBox("Failed", $"Не удалось подключиться к сервису с адресом {surfaceRecognitionServiceAddress}");
                    Log.Warning("MainViewModel.CheckHealthAsync: Error; Message: {@Message}", $"Не удалось подключиться к сервису с адресом {surfaceRecognitionServiceAddress}");
                    LogJournalViewModel.logString += ("MainViewModel.CheckHealthAsync: Error; Message: {@Message}", $"Не удалось подключиться к сервису с адресом {surfaceRecognitionServiceAddress}\n");
                }
            }
            catch
            {
                _serviceProvider.GetRequiredService<NavigationViewModel>().ConnectionStatus = Brushes.Red;
                _videoEventJournalViewModel.ClearUI();
                ShowMessageBox("Failed", $"Не удалось подключиться к сервису с адресом {surfaceRecognitionServiceAddress}");
                Log.Warning("MainViewModel.CheckHealthAsync: Error; Message: {@Message}", $"Не удалось подключиться к сервису с адресом {surfaceRecognitionServiceAddress}");
                LogJournalViewModel.logString += ("MainViewModel.CheckHealthAsync: Error; Message: {@Message}", $"Не удалось подключиться к сервису с адресом {surfaceRecognitionServiceAddress}\n");
            }
        }
    }

    private async void StartNeuralServiceWatcher()
    {
        Log.Debug("MainViewModel.StartNeuralServiceWatcher: Start health check");
        LogJournalViewModel.logString += "MainViewModel.StartNeuralServiceWatcher: Start health check\n";
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
                            LogJournalViewModel.logString += ("MainViewModel.StartNeuralServiceWatcher: Error; Message: {@Message}", "Пропало соединение с нейросетевым сервисом, попробуйте подключиться еще раз.\n");
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
                        LogJournalViewModel.logString += ("MainViewModel.StartNeuralServiceWatcher: Error; Message: {@Message}", "Пропало соединение с нейросетевым сервисом, попробуйте подключиться еще раз.\n");
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
        messageBoxStandardWindow.ShowAsync();
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

        public List<Bitmap> frames { get; set; }

        public AvaloniaList<AvaloniaList<RectItem>> rectitems { get; set; } = new();

        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
    #endregion
}
