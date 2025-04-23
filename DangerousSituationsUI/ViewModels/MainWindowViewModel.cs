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
using LibVLCSharp.Shared;
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

namespace DangerousSituationsUI.ViewModels;

public class MainViewModel : ReactiveObject, IRoutableViewModel
{
    #region Private Fields

    private string _currentFileName;

    private FilesService _filesService;

    private VideoService _videoService;

    private RectItemService _rectItemService;

    private readonly ConfigurationService _configurationService;

    private IServiceProvider _serviceProvider;

    private bool _isLoading;

    private int _progressPercentage;

    private bool _areButtonsEnabled;

    private bool _areConnectButtonEnabled = true;

    private AvaloniaList<LegendItem> _legendItems;

    private AvaloniaList<string> _filesNames;

    private AvaloniaList<IStorageFile> _files;

    private LibVLC _libVLC = new LibVLC();

    private MediaPlayer _mediaPlayer;

    private bool _canPlay;

    private bool _canPause;

    private bool _canStop;

    private string _playButtonColor;

    private string _pauseButtonColor;

    private string _stopButtonColor;

    private string _videoTime;

    private TelegramBotAPI _telegramBotApi;
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
    public ReactiveCommand<Unit, Unit> SendImageCommand { get; }

    public ReactiveCommand<Unit, Unit> SendFolderCommand { get; }

    public ReactiveCommand<Unit, Unit> SendVideoCommand { get; }

    public ReactiveCommand<Unit, Unit> ConnectCommand { get; }

    public ReactiveCommand<Unit, Unit> PlayCommand { get; }

    public ReactiveCommand<Unit, Unit> PauseCommand { get; }

    public ReactiveCommand<Unit, Unit> StopCommand { get; }
    #endregion

    #region Properties
    public string CurrentFileName
    {
        get => _currentFileName;
        set => this.RaiseAndSetIfChanged(ref _currentFileName, value);
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

    public MediaPlayer MediaPlayer
    {
        get => _mediaPlayer;
        set => this.RaiseAndSetIfChanged(ref _mediaPlayer, value);
    }

    public AvaloniaList<string> FilesNames
    {
        get => _filesNames;
        set => this.RaiseAndSetIfChanged(ref _filesNames, value);
    }

    public bool CanPlay
    {
        get => _canPlay;
        set => this.RaiseAndSetIfChanged(ref _canPlay, value);
    }

    public bool CanPause
    {
        get => _canPause;
        set => this.RaiseAndSetIfChanged(ref _canPause, value);
    }

    public bool CanStop
    {
        get => _canStop;
        set => this.RaiseAndSetIfChanged(ref _canStop, value);
    }

    public string PlayButtonColor
    {
        get => _playButtonColor;
        set => this.RaiseAndSetIfChanged(ref _playButtonColor, value);
    }

    public string StopButtonColor
    {
        get => _stopButtonColor;
        set => this.RaiseAndSetIfChanged(ref _stopButtonColor, value);
    }

    public string PauseButtonColor
    {
        get => _pauseButtonColor;
        set => this.RaiseAndSetIfChanged(ref _pauseButtonColor, value);
    }

    public string VideoTime
    {
        get => _videoTime;
        set => this.RaiseAndSetIfChanged(ref _videoTime, value);
    }
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
        TelegramBotAPI telegramBotApi)
    {
        HostScreen = screen;
        _filesService = filesService;
        _videoService = videoService;
        _rectItemService = rectItemService;
        _serviceProvider = serviceProvider;
        _videoEventJournalViewModel = videoEventJournalViewModel;
        _configurationService = configurationService;
        _telegramBotApi = telegramBotApi;

        _telegramBotApi.StartBotAsync();

        AreButtonsEnabled = false;

        _legendItems = new AvaloniaList<LegendItem>
        {
            new LegendItem { ClassName = "human", Color = "Green" },
            new LegendItem { ClassName = "wind/sup-board", Color = "Red" },
            new LegendItem { ClassName = "bouy", Color = "Blue" },
            new LegendItem { ClassName = "sailboat", Color = "Yellow" },
            new LegendItem { ClassName = "kayak", Color = "Purple" }
        };

        SetPauseFlag(false);
        SetPlayFlag(false);
        SetStopFlag(false);

        VideoTime = GetVideoTimeString(0);

        ConnectCommand = ReactiveCommand.CreateFromTask(CheckHealthAsync);
        SendFolderCommand = ReactiveCommand.CreateFromTask(OpenFolderAsync);
        SendVideoCommand = ReactiveCommand.CreateFromTask(OpenVideoAsync);
        PlayCommand = ReactiveCommand.Create(PlayVideo);
        PauseCommand = ReactiveCommand.Create(PauseVideo);
        StopCommand = ReactiveCommand.Create(StopVideo);
    }
    #endregion

    #region Command Methods
    private async Task OpenFolderAsync()
    {
        Log.Information("Start sending folder");
        LogJournalViewModel.logString += "Start sending folder\n";
        Log.Debug("MainViewModel.OpenFolderAsync: Start");
        LogJournalViewModel.logString += "MainViewModel.OpenFolderAsync: Start\n";
        AreButtonsEnabled = false;
        try
        {
            var files = await _filesService.OpenVideoFolderAsync();
            if (files != null)
            {
                foreach (var file in files)
                {
                    CurrentFileName = file.Name;
                    await InitFramesAsync(file);
                    CurrentFileName = string.Empty;
                    IsLoading = false;
                    ProgressPercentage = 0;
                }
            }
        }
        catch
        {
            ShowMessageBox("Error", "В выбранной директории отсутcтвуют изображения или пристуствуют файлы с недопустимым расширением.");
            Log.Warning("MainViewModel.OpenFolderAsync: Error; Message: В выбранной директории отсутcnвуют изображения или пристуствуют файлы с недопустимым расширением.");
            LogJournalViewModel.logString += "MainViewModel.OpenFolderAsync: Error; Message: В выбранной директории отсутcnвуют изображения или пристуствуют файлы с недопустимым расширением.\n";
            return;
        }
        finally { AreButtonsEnabled = true; }
    }

    private async Task OpenVideoAsync()
    {
        Log.Information("Start sending video"); 
        LogJournalViewModel.logString += "Start sending video\n";
        Log.Debug("MainViewModel.OpenVideoAsync: Start");
        LogJournalViewModel.logString += "MainViewModel.OpenVideoAsync: Start\n";
        _videoEventJournalViewModel.EventResults = new AvaloniaList<string>();
        AreButtonsEnabled = false;
        try
        {
            var file = await _filesService.OpenVideoFileAsync();

            if (file != null)
            {
                await InitFramesAsync(file);
                _files = [file];

                await _videoEventJournalViewModel.FillComboBox();

                using var media = new Media(_libVLC, _files[0].Path);
                SetMediaPlayer(media);
            }

            IsLoading = false;
            ProgressPercentage = 0;

            Log.Information("End sending video");
            LogJournalViewModel.logString += "End sending video\n";
            Log.Debug("MainViewModel.OpenVideoAsync: Done");
            LogJournalViewModel.logString += "MainViewModel.OpenVideoAsync: Done\n";
        }
        catch
        {
            string message = "Возникла ошибка на этапе обработки видео.";
            ShowMessageBox("Error", message);
            Log.Warning("MainViewModel.OpenFolderAsync: Error; Message: {Message}", message);
            LogJournalViewModel.logString += ("MainViewModel.OpenFolderAsync: Error; Message: {message}\n",message);
        }
        finally { AreButtonsEnabled = true; }
    }

    private void PlayVideo()
    {
        MediaPlayer.Play();
    }

    private void PauseVideo()
    {
        MediaPlayer.Pause();
    }

    private void StopVideo()
    {
        MediaPlayer?.Stop();

        VideoTime = GetVideoTimeString(0);

        SetPlayFlag(true);
        SetPauseFlag(false);
        SetStopFlag(false);
    }
    #endregion

    private void ResetUI()
    {
        CurrentFileName = String.Empty;
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

        List<FrameNDetections> frameNDetections = new List<FrameNDetections>();
        int totalFiles = frames.Count();
        for (int idx = 0; idx < totalFiles; idx++)
        {
            var results = await GetFrameDetectionResultsAsync(frames[idx], idx + 1);
            itemsLists.Add(results);
            ProgressPercentage = (int)((idx + 1) / (double)totalFiles * 100);
            frameNDetections.Add(new FrameNDetections
            {
                Frame = frames[idx],
                Detections = results
            });
        }

        await SaveDataIntoDatabase(file, frameNDetections);

        CurrentFileName = file.Name;
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

            var frame = new Frame
            {
                FrameData = frameBytes,
                CreatedAt = DateTime.UtcNow,
                Detections = detections
            };

            videoModel.Frames.Add(frame);
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
                await SaveRecognitionResultAsync(det);
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
    #endregion

    #region MediaPlayer Methods
    public void SetMediaPlayer(Media media)
    {
        StopVideo();

        MediaPlayer = new(media);

        MediaPlayer.Playing += (object? sender, EventArgs args) =>
        {
            SetPauseFlag(true);
            SetPlayFlag(false);
            SetStopFlag(true);
        };
        MediaPlayer.Paused += (object? sender, EventArgs args) =>
        {
            SetPauseFlag(false);
            SetPlayFlag(true);
            SetStopFlag(true);
        };
        MediaPlayer.EndReached += (object? sender, EventArgs args) =>
        {
            SetPauseFlag(false);
            SetPlayFlag(false);
            SetStopFlag(true);
        };
        MediaPlayer.TimeChanged += (object? sender, MediaPlayerTimeChangedEventArgs args) =>
        {
            VideoTime = GetVideoTimeString(args.Time);
        };
    }

    private void SetPlayFlag(bool flag)
    {
        CanPlay = flag;
        PlayButtonColor = SetButtonColor(flag);
    }

    private void SetStopFlag(bool flag)
    {
        CanStop = flag;
        StopButtonColor = SetButtonColor(flag);
    }

    private void SetPauseFlag(bool flag)
    {
        CanPause = flag;
        PauseButtonColor = SetButtonColor(flag);
    }

    private string SetButtonColor(bool flag) => flag ? "LightGray" : "Gray";

    private string GetVideoTimeString(long ms) => TimeSpan.FromMilliseconds(ms).ToString();
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

    #region Data Base Methods
    private async Task SaveRecognitionResultAsync(RecognitionResult recognitionResult)
    {
        Log.Debug("MainViewModel.SaveRecognitionResultAsync: Start");
        LogJournalViewModel.logString += "MainViewModel.SaveRecognitionResultAsync: Start\n";
        using ApplicationContext db = _serviceProvider.GetRequiredService<ApplicationContext>();
        db.RecognitionResults.AddRange(recognitionResult);
        await db.SaveChangesAsync();
        Log.Debug("MainViewModel.SaveRecognitionResultAsync: Done");
        LogJournalViewModel.logString += "MainViewModel.SaveRecognitionResultAsync: Done\n"; 
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
    #endregion
}
