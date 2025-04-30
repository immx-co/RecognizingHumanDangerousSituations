using LibVLCSharp.Shared;
using MsBox.Avalonia;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading;

namespace DangerousSituationsUI.ViewModels;

public class VideoPlayerViewModel : ReactiveObject, IRoutableViewModel
{
    #region Private Fields
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
    public ReactiveCommand<Unit, Unit> PlayCommand { get; }

    public ReactiveCommand<Unit, Unit> PauseCommand { get; }

    public ReactiveCommand<Unit, Unit> StopCommand { get; }
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
    #endregion


    #region Constructors
    public VideoPlayerViewModel(
        IScreen screen,
        VideoEventJournalViewModel videoEventJournalViewModel)
    {
        HostScreen = screen;
        _videoEventJournalViewModel = videoEventJournalViewModel;

        AreButtonsEnabled = false;

        SetPauseFlag(false);
        SetPlayFlag(false);
        SetStopFlag(false);

        VideoTime = GetVideoTimeString(0);

        PlayCommand = ReactiveCommand.Create(PlayVideo);
        PauseCommand = ReactiveCommand.Create(PauseVideo);
        StopCommand = ReactiveCommand.Create(StopVideo);
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
        public string Name { get; set; }
        public string Path { get; set; }
    }
    #endregion

}
