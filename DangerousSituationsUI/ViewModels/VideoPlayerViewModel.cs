using Avalonia.Media;
using ClassLibrary.Database;
using ClassLibrary.Database.Models;
using ClassLibrary.Datacontracts;
using ClassLibrary.Services;
using DangerousSituationsUI.Services;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using ReactiveUI;
using Sprache;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using static DangerousSituationsUI.ViewModels.VideoEventJournalViewModel;

namespace DangerousSituationsUI.ViewModels;

public class VideoPlayerViewModel : ReactiveObject, IRoutableViewModel
{
    #region Private Fields
    private IServiceProvider _serviceProvider;

    private string _currentFileName;

    private bool _areButtonsEnabled;

    private LibVLC _libVLC = new LibVLC();

    private MediaPlayer _mediaPlayer;

    private bool _canPlay;

    private bool _canPause;

    private bool _canStop;

    private string _playButtonColor;

    private string _pauseButtonColor;

    private string _stopButtonColor;

    private string _videoTime;

    private ObservableCollection<VideoItem> _videoItems = new();

    private VideoItem _selectedVideoItem;

    private List<DetectionItem> _detections = new List<DetectionItem>();

    private TimeSpan _clipStartTime;

    private TimeSpan _clipEndTime;

    private ExportService _exportService;

    private FilesService _filesService;
    #endregion


    #region Public Fields
    public VideoEventJournalViewModel _videoEventJournalViewModel;
    #endregion


    #region View Model Settings
    public ObservableCollection<RectItem> Rectangles { get; }
        = new ObservableCollection<RectItem>();
    public IScreen HostScreen { get; }

    public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

    public CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    #endregion


    #region Commands
    public ReactiveCommand<Unit, Unit> PlayCommand { get; }

    public ReactiveCommand<Unit, Unit> PauseCommand { get; }

    public ReactiveCommand<Unit, Unit> StopCommand { get; }

    public ReactiveCommand<Unit, Unit> ExportClipCommand { get; }
    #endregion


    #region Properties
    public LibVLC LibVLCInstance => _libVLC;

    public ObservableCollection<VideoItem> VideoItems
    {
        get => _videoItems;
        set => this.RaiseAndSetIfChanged(ref _videoItems, value);
    }

    public VideoItem SelectedVideoItem
    {
        get => _selectedVideoItem;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedVideoItem, value);
            if (value != null)
            {
                var media = new Media(LibVLCInstance, value.Path, FromType.FromPath);
                SetMediaPlayer(media); 
                LoadDetections(value.GetFullName());
            }
        }
    }

    public string CurrentFileName
    {
        get => _currentFileName;
        set => this.RaiseAndSetIfChanged(ref _currentFileName, value);
    }

    public bool AreButtonsEnabled
    {
        get => _areButtonsEnabled;
        private set => this.RaiseAndSetIfChanged(ref _areButtonsEnabled, value);
    }

    public MediaPlayer MediaPlayer
    {
        get => _mediaPlayer;
        set => this.RaiseAndSetIfChanged(ref _mediaPlayer, value);
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

    public TimeSpan ClipStartTime
    {
        get => _clipStartTime;
        set => this.RaiseAndSetIfChanged(ref _clipStartTime, value);
    }

    public TimeSpan ClipEndTime
    {
        get => _clipEndTime;
        set => this.RaiseAndSetIfChanged(ref _clipEndTime, value);
    }

    public string ClipStartTimeString
    {
        get => ClipStartTime.ToString(@"hh\:mm\:ss");
        set
        {
            if (TimeSpan.TryParse(value, out var ts))
                ClipStartTime = ts;
        }
    }

    public string ClipEndTimeString
    {
        get => ClipEndTime.ToString(@"hh\:mm\:ss");
        set
        {
            if (TimeSpan.TryParse(value, out var ts))
                ClipEndTime = ts;
        }
    }
    #endregion


    #region Constructors
    public VideoPlayerViewModel(
        IScreen screen, 
        IServiceProvider serviceProvider,
        VideoEventJournalViewModel videoEventJournalViewModel,
        ExportService exportService,
        FilesService filesService)
    {
        HostScreen = screen;
        _videoEventJournalViewModel = videoEventJournalViewModel;
        _serviceProvider = serviceProvider;
        _exportService = exportService;
        _filesService = filesService;

        AreButtonsEnabled = false;

        SetPauseFlag(false);
        SetPlayFlag(false);
        SetStopFlag(false);

        VideoTime = GetVideoTimeString(0);

        PlayCommand = ReactiveCommand.Create(PlayVideo);
        PauseCommand = ReactiveCommand.Create(PauseVideo);
        StopCommand = ReactiveCommand.Create(StopVideo);
        ExportClipCommand = ReactiveCommand.CreateFromTask(ExportClipAsync);
    }
    #endregion


    #region Command Methods
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
        ClearRectangles();

        VideoTime = GetVideoTimeString(0);

        SetPlayFlag(true);
        SetPauseFlag(false);
        SetStopFlag(false);
    }

    private async Task ExportClipAsync()
    {
        if (SelectedVideoItem == null)
        {
            ShowMessageBox("Error", "Видео не выбрано.");
            return;
        }

        if (ClipEndTime <= ClipStartTime)
        {
            ShowMessageBox("Error", "Время начала должно быть меньше времени конца.");
            return;
        }

        var videoDuration = await GetVideoDurationAsync(SelectedVideoItem.Path);

        if (ClipEndTime > videoDuration)
        {
            ShowMessageBox("Error", $"Время конца превышает длину видео: {videoDuration}");
            return;
        }

        try
        {
            var result = await _exportService.ExportClipAndDetectionsAsync(
                SelectedVideoItem.Guid,
                SelectedVideoItem.Path,
                ClipStartTime,
                ClipEndTime
            );

            if (result is null)
            {
                return;
            }

            var (clipPath, jsonPath) = result.Value;
            ShowMessageBox("Success", $"Видео сохранено:\n{clipPath}\nДетекции сохранены:\n{jsonPath}");
        }
        catch (Exception ex)
        {
            ShowMessageBox("Error", $"Ошибка экспорта: {ex.Message}");
        }
    }

    #endregion

    private void ResetUI()
    {
        CurrentFileName = String.Empty;
    }

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
            var result = SelectedVideoItem;
            UpdateRectangles(TimeSpan.FromMilliseconds(args.Time));
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

    private void AddRectangle(string name)
    {
        using ApplicationContext db = _serviceProvider.GetRequiredService<ApplicationContext>();

        var dbVideo = db.Videos.Where(video => video.VideoName == name).FirstOrDefault();

        if (dbVideo is null)
        {
            return;
        }

        var dbFrame = db.Frames.Where(frame => frame.VideoId == dbVideo.VideoId ).ToList();

        if (dbFrame is null)
        {
            return;
        }

        var dbDetection = new List<Detection>();

        foreach (var frame in dbFrame)
        {

            dbDetection = db.Detections.Where(detection => detection.FrameId == frame.FrameId).ToList();

            foreach (var detection in dbDetection)
            {
                Rectangles.Add(new RectItem
                {
                    X = detection.X,
                    Y = detection.Y,
                    Width = detection.Width,
                    Height = detection.Height,
                    Color = detection.ClassName switch
                    {
                        "Standing" => "Green",
                        "Lying" => "Red",
                    },

                });
            }
        }
    }

    private  void ClearRectangles()
    {
        Rectangles.Clear();
    }

    private void LoadDetections(string fullVideoName)
    {
        var match = Regex.Match(fullVideoName, @"^(.*?)\s*\((.*?)\)$");

        string? videoName = String.Empty;
        string? stringGuid = String.Empty;
        if (match.Success)
        {
            videoName = match.Groups[1].Value.Trim();
            stringGuid = match.Groups[2].Value.Trim();
        }
        else
        {
            return;
        }

        using ApplicationContext db = _serviceProvider.GetRequiredService<ApplicationContext>();

        var dbVideo = db.Videos.FirstOrDefault(v => v.VideoId == Guid.Parse(stringGuid));
        if (dbVideo == null) return;

        DateTime videoStartTime = dbVideo.CreatedAt;

        _detections = db.Frames
            .Where(f => f.VideoId == dbVideo.VideoId)
            .Join(db.Detections,
                frame => frame.FrameId,
                detection => detection.FrameId,
                (frame, detection) => new { frame, detection })
            .AsEnumerable() 
            .Select(x => new DetectionItem
            {
                Time = x.frame.FrameTime,
                Rect = new RectItem
                {
                    X = x.detection.X,
                    Y = x.detection.Y,
                    Width = x.detection.Width,
                    Height = x.detection.Height,
                    Color = x.detection.ClassName switch
                    {
                        "Standing" => "Green",
                        "Lying" => "Red",
                        _ => "Yellow"
                    }
                }
            })
            .ToList();
    }

    private void UpdateRectangles(TimeSpan currentVideoTime)
    {
        var nearestFrameDetections = _detections
            .Where(d => d.Time <= currentVideoTime && currentVideoTime - d.Time <= TimeSpan.FromMilliseconds(250))
            .GroupBy(d => d.Time)
            .OrderByDescending(g => g.Key)
            .FirstOrDefault();

        if (nearestFrameDetections != null)
        {
            ClearRectangles();
            // Добавляем все детекции из этого кадра
            foreach (var detection in nearestFrameDetections)
            {
                Rectangles.Add(detection.Rect);
            }
        }
    }

    public async Task<TimeSpan?> GetVideoDurationAsync(string videoPath)
    {
        using var media = new Media(LibVLCInstance, videoPath, FromType.FromPath);

        await media.Parse(MediaParseOptions.ParseLocal);

        if (media.Duration > 0)
            return TimeSpan.FromMilliseconds(media.Duration);
        else
            return null;
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
    public class VideoItem
    {
        public Guid Guid { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        public string GetFullName()
        {
            return $"{this.Name} ({this.Guid.ToString()})";
        }
    }

    public class DetectionItem
    {
        public TimeSpan Time { get; set; }       // Время детекции относительно видео

        public RectItem Rect { get; set; }       // Прямоугольник детекции
    }
    #endregion

}
