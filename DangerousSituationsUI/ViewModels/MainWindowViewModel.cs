using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ClassLibrary.Database;
using ClassLibrary.Database.Models;
using ClassLibrary.Services;
using ClassLibrary.Datacontracts;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia;
using DangerousSituationsUI.Services;
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
using ClassLibrary.Repository;

namespace DangerousSituationsUI.ViewModels;

public class MainViewModel : ReactiveObject, IRoutableViewModel
{
    #region Private Fields
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

    private bool _isVideoSelected = false;

    private AvaloniaList<RectItem> _rectItems;

    private AvaloniaList<AvaloniaList<RectItem>> _rectItemsLists;

    private int _currentNumberOfImage;

    private AvaloniaList<LegendItem> _legendItems;

    private int _currentNumberOfFrame;

    private List<AvaloniaList<string>> _detections;
    #endregion

    #region Public Fields
    public EventJournalViewModel _eventJournalViewModel;

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

    public ReactiveCommand<Unit, Unit> SendImageCommand { get; }

    public ReactiveCommand<Unit, Unit> SendFolderCommand { get; }

    public ReactiveCommand<Unit, Unit> SendVideoCommand { get; }

    public ReactiveCommand<Unit, Unit> ConnectCommand { get; }
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
    #endregion

    #region Constructors
    public MainViewModel(
        IScreen screen,
        FilesService filesService,
        ConfigurationService configurationService,
        IServiceProvider serviceProvider,
        VideoService videoService,
        RectItemService rectItemService,
        EventJournalViewModel eventJournalViewModel,
        VideoEventJournalViewModel videoEventJournalViewModel)
    {
        HostScreen = screen;
        _filesService = filesService;
        _videoService = videoService;
        _rectItemService = rectItemService;
        _serviceProvider = serviceProvider;
        _eventJournalViewModel = eventJournalViewModel;
        _videoEventJournalViewModel = videoEventJournalViewModel;
        _configurationService = configurationService;

        ConnectionStatus = Brushes.Gray;
        AreButtonsEnabled = false;

        _legendItems = new AvaloniaList<LegendItem>
        {
            new LegendItem { ClassName = "human", Color = "Green" },
            new LegendItem { ClassName = "wind/sup-board", Color = "Red" },
            new LegendItem { ClassName = "bouy", Color = "Blue" },
            new LegendItem { ClassName = "sailboat", Color = "Yellow" },
            new LegendItem { ClassName = "kayak", Color = "Purple" }
        };

        CanSwitchImages = false;

        ConnectCommand = ReactiveCommand.CreateFromTask(CheckHealthAsync);
        SendImageCommand = ReactiveCommand.CreateFromTask(OpenImageFileAsync);
        SendFolderCommand = ReactiveCommand.CreateFromTask(OpenFolderAsync);
        SendVideoCommand = ReactiveCommand.CreateFromTask(OpenVideoAsync);
        ImageBackCommand = ReactiveCommand.Create(Previous);
        ImageForwardCommand = ReactiveCommand.Create(Next);
    }
    #endregion

    #region Command Methods
    private async Task OpenImageFileAsync()
    {
        Log.Information("Start sending image file");
        Log.Debug("MainViewModel.OpenImageFileAsync: Start");

        try
        {
            var file = await _filesService.OpenImageFileAsync();
            if (file != null)
            {
                await InitImagesAsync(new List<IStorageFile> { file });
                CanSwitchImages = false;
                _isVideoSelected = false;
                FrameTitle = String.Empty;
                InitEventJournal();
            }
        }
        finally
        {
            IsLoading = false;
            ProgressPercentage = 0;
            Log.Information("End sending image file");
            Log.Debug("MainViewModel.OpenImageFileAsync: Done");
        }
    }

    private async Task OpenFolderAsync()
    {
        Log.Information("Start sending folder");
        Log.Debug("MainViewModel.OpenFolderAsync: Start");

        try
        {
            var files = await _filesService.OpenImageFolderAsync();
            if (files != null)
            {
                await InitImagesAsync(files);
                CanSwitchImages = true;
                _isVideoSelected = false;
                FrameTitle = String.Empty;
                InitEventJournal();
            }
        }
        catch
        {
            ShowMessageBox("Error", "В выбранной директории отсутcnвуют изображения или пристуствуют файлы с недопустимым расширением.");
            Log.Warning("MainViewModel.OpenFolderAsync: Error; Message: В выбранной директории отсутcnвуют изображения или пристуствуют файлы с недопустимым расширением.");
            return;
        }
        finally
        {
            IsLoading = false;
            ProgressPercentage = 0;
            Log.Information("End sending folder");
            Log.Debug("MainViewModel.OpenFolderAsync: Done");
        }
    }

    private async Task OpenVideoAsync()
    {
        Log.Information("Start sending video");
        Log.Debug("MainViewModel.OpenVideoAsync: Start");
        _videoEventJournalViewModel.EventResults = new AvaloniaList<string>();
        try
        {
            var file = await _filesService.OpenVideoFileAsync();
            if (file != null)
            {
                await InitFramesAsync(file);
                CanSwitchImages = true;
                _isVideoSelected = true;
                FrameTitle = $"{_currentNumberOfFrame + 1} / {_frames.Count}";
            }
        }
        finally
        {
            IsLoading = false;
            ProgressPercentage = 0;
            await _videoEventJournalViewModel.FillComboBox();
            Log.Information("End sending video");
            Log.Debug("MainViewModel.OpenVideoAsync: Done");
        }
    }

    private void Next()
    {
        if (_isVideoSelected) NextFrame();
        else NextImage();
    }

    private void Previous()
    {
        if (_isVideoSelected) PreviousFrame();
        else PreviousImage();
    }
    #endregion

    #region Image Methods
    private async Task InitImagesAsync(List<IStorageFile> files)
    {
        Log.Debug("MainViewModel.InitImagesAsync: Start");
        IsLoading = true;
        var itemsLists = new AvaloniaList<AvaloniaList<RectItem>>();
        var filesBitmap = new List<Bitmap>();
        int totalFiles = files.Count;
        _detections = new();

        for (int idx = 0; idx < totalFiles; idx++)
        {
            var file = files[idx];
            var fileBitmap = new Bitmap(await file.OpenReadAsync());
            filesBitmap.Add(fileBitmap);

            var results = await GetImageDetectionResultsAsync(file, fileBitmap);
            itemsLists.Add(results);

            ProgressPercentage = (int)((idx + 1) / (double)totalFiles * 100);
        }

        _imageFilesBitmap = filesBitmap;
        _rectItemsLists = itemsLists;
        _imageFiles = files;

        _currentNumberOfImage = 0;
        SetImage();
        Log.Debug("MainViewModel.InitImagesAsync: End");
    }

    private async Task<AvaloniaList<RectItem>> GetImageDetectionResultsAsync(IStorageFile file, Bitmap fileBitmap)
    {
        Log.Debug("MainViewModel.GetImageDetectionResultsAsync: Start");
        List<RecognitionResult> detections = await GetImageRecognitionResultsAsync(file);
        var items = new AvaloniaList<RectItem>();
        var detectionList = new AvaloniaList<string>();
        foreach (RecognitionResult det in detections)
        {
            try
            {
                items.Add(_rectItemService.InitRect(det, fileBitmap));
                await SaveRecognitionResultAsync(det);
                string eventLine = $"Class: {det.ClassName}; x: {det.X}; y: {det.Y}; width: {det.Width}; height: {det.Height}";
                detectionList.Add(eventLine);
            }
            catch (Exception ex)
            {
                ShowMessageBox("Error", $"Ошибка при обработке детекции: {ex.Message}");
                Log.Warning("MainViewModel.GetImageDetectionResultsAsync: Error; Message: {@Message}", ex.Message);
            }
        }
        Log.Debug("MainViewModel.GetImageDetectionResultsAsync: Done");

        _detections.Add(detectionList);

        return items;
    }

    private async Task<List<RecognitionResult>> GetImageRecognitionResultsAsync(IStorageFile file)
    {
        Log.Debug("MainViewModel.GetImageRecognitionResultsAsync: Start");
        string surfaceRecognitionServiceAddress = _configurationService.GetConnectionString("srsStringConnection");
        using (var client = new HttpClient())
        {
            try
            {
                using (var imageStream = await file.OpenReadAsync())
                {
                    var content = new MultipartFormDataContent();
                    var imageContent = new StreamContent(imageStream);
                    imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    content.Add(imageContent, "image", file.Name);

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
                        ShowMessageBox("Error", $"Ошибка при отправке изображения: {response.StatusCode}");
                        Log.Warning("MainViewModel.GetImageRecognitionResultsAsync: Error; Message: {@Message}", $"Ошибка при отправке изображения: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox("Error", $"Ошибка при отправке изображения: {ex.Message}");
                Log.Warning("MainViewModel.GetImageRecognitionResultsAsync: Error; Message: {@Message}", ex.Message);
            }
        }
        Log.Debug("MainViewModel.GetImageRecognitionResultsAsync: Done");
        return new List<RecognitionResult>();
    }

    private void NextImage()
    {
        if (_currentNumberOfImage < _imageFilesBitmap.Count - 1) _currentNumberOfImage++;
        else _currentNumberOfImage = 0;

        SetImage();
    }

    private void PreviousImage()
    {
        if (_currentNumberOfImage > 0) _currentNumberOfImage--;
        else _currentNumberOfImage = _imageFilesBitmap.Count - 1;

        SetImage();
    }

    private void SetImage()
    {
        CurrentFileName = _imageFiles[_currentNumberOfImage].Name;
        CurrentImage = _imageFilesBitmap[_currentNumberOfImage];
        RectItems = _rectItemsLists[_currentNumberOfImage];
    }

    private void ResetUI()
    {
        RectItems = new AvaloniaList<RectItem>();
        CurrentImage = null;
        CurrentFileName = String.Empty;
        FrameTitle = String.Empty;
    }
    #endregion

    #region Video Methods
    private async Task InitializeVideoEventJournalWindow()
    {
        await _videoEventJournalViewModel.FillComboBox();
    }

    private async Task InitFramesAsync(IStorageFile file)
    {
        Log.Debug("MainViewModel.InitFramesAsync: Start");
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

        _videoFile = file;
        _rectItemsLists = itemsLists;
        _frames = frames;

        CurrentFileName = file.Name;
        _currentNumberOfFrame = 0;
        SetFrame();
        Log.Debug("MainViewModel.InitFramesAsync: End");
    }

    private async Task<Video> SaveDataIntoDatabase(IStorageFile videoFile, List<FrameNDetections> framesNDetections)
    {
        Log.Debug("MainViewModel.SaveDataIntoDatabaseAsync: Start");

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
        return addedVideo;
    }
    private async Task<AvaloniaList<RectItem>> GetFrameDetectionResultsAsync(Bitmap frame, int numberOfFrame)
    {
        Log.Debug("MainViewModel.GetFrameDetectionResultsAsync: Start");
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
    #endregion

    #region Client Methods
    private async Task CheckHealthAsync()
    {
        ConnectionStatus = Brushes.Red;
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
                        ConnectionStatus = Brushes.Green;
                        AreButtonsEnabled = true;
                        AreConnectButtonEnabled = false;
                        ShowMessageBox("Success", $"Сервис доступен. Статус: {healthResponse.StatusCode}");
                        Task.Run(() => StartNeuralServiceWatcher());
                        Task.Run(InitializeVideoEventJournalWindow);
                        Log.Debug("MainViewModel.CheckHealthAsync: Health checked");
                    }
                    else
                    {
                        ConnectionStatus = Brushes.Red;
                        _videoEventJournalViewModel.ClearUI();
                        ShowMessageBox("Failed", $"Сервис недоступен. Статус: {healthResponse?.StatusCode}");
                        Log.Warning("MainViewModel.CheckHealthAsync: Error; Message: {@Message}", $"Сервис недоступен. Статус: {healthResponse?.StatusCode}");
                    }
                }
                else
                {
                    ConnectionStatus = Brushes.Red;
                    _videoEventJournalViewModel.ClearUI();
                    ShowMessageBox("Failed", $"Не удалось подключиться к сервису с адресом {surfaceRecognitionServiceAddress}");
                    Log.Warning("MainViewModel.CheckHealthAsync: Error; Message: {@Message}", $"Не удалось подключиться к сервису с адресом {surfaceRecognitionServiceAddress}");
                }
            }
            catch
            {
                ConnectionStatus = Brushes.Red;
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
                            ConnectionStatus = Brushes.Red;
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
                        ConnectionStatus = Brushes.Red;
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

    #region Data Base Methods
    private async Task SaveRecognitionResultAsync(RecognitionResult recognitionResult)
    {
        Log.Debug("MainViewModel.SaveRecognitionResultAsync: Start");
        using ApplicationContext db = _serviceProvider.GetRequiredService<ApplicationContext>();
        db.RecognitionResults.AddRange(recognitionResult);
        await db.SaveChangesAsync();
        Log.Debug("MainViewModel.SaveRecognitionResultAsync: Done");
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

    #region Event Journal Methods
    private void InitEventJournal()
    {
        var dictioanary = new Dictionary<string, Bitmap>();
        AvaloniaList<string> names = new();
        var eventResults = new Dictionary<string, AvaloniaList<string>>();
        for (int i = 0; i < _imageFiles.Count; i++)
        {
            dictioanary.Add(_imageFiles[i].Name, _imageFilesBitmap[i]);
            names.Add(_imageFiles[i].Name);
            eventResults.Add(_imageFiles[i].Name, _detections[i]);
        }
        _eventJournalViewModel.ImagesDictionary = dictioanary;
        _eventJournalViewModel.ImageNames = names;
        _eventJournalViewModel.EventResults = eventResults;
        _eventJournalViewModel.SelectedImageName = _eventJournalViewModel.ImageNames[0];
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
