using Avalonia.Collections;
using ClassLibrary.Database;
using ClassLibrary.Database.Models;
using ClassLibrary.Services;
using DangerousSituationsUI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DangerousSituationsUI.ViewModels;

public class VideoEventJournalViewModel : ReactiveObject, IRoutableViewModel
{
    #region Private Fields
    private IServiceProvider _serviceProvider;

    private AvaloniaList<string> _eventResults;

    private AvaloniaList<RectItem> _rectItems;

    private Avalonia.Media.Imaging.Bitmap _currentImage;

    private RectItemService _rectItemService;

    private string _title;

    private string _selectedEventResult;

    private List<VideoItemModel>? _videoItems;

    private VideoItemModel _selectedVideoItem;

    private AvaloniaList<LegendItem> _legendItems;
    #endregion

    #region View Model Settings
    public IScreen HostScreen { get; }
    public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

    public CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    #endregion

    #region Properties
    public AvaloniaList<string> EventResults
    {
        get => _eventResults;
        set => this.RaiseAndSetIfChanged(ref _eventResults, value);
    }

    public string SelectedEventResult
    {
        get => _selectedEventResult;
        set
        {
            _selectedEventResult = value;
            if (_selectedEventResult != null) Render();
            else Clear();
        }
    }

    public AvaloniaList<RectItem> RectItems
    {
        get => _rectItems;
        set => this.RaiseAndSetIfChanged(ref _rectItems, value);
    }

    public Avalonia.Media.Imaging.Bitmap CurrentImage
    {
        get => _currentImage;
        set => this.RaiseAndSetIfChanged(ref _currentImage, value);
    }

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public AvaloniaList<LegendItem> LegendItems
    {
        get => _legendItems;
        set => this.RaiseAndSetIfChanged(ref _legendItems, value);
    }

    public List<VideoItemModel>? VideoItems
    {
        get => _videoItems;
        set => this.RaiseAndSetIfChanged(ref _videoItems, value);
    }

    public VideoItemModel SelectedVideoItem
    {
        get => _selectedVideoItem;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedVideoItem, value);

            if (value != null)
            {
                DisplayVideoImages(value);
            }
        }
    }

    public bool IsVideoProcessing = false;
    #endregion

    #region Constructor
    public VideoEventJournalViewModel(IScreen screen, IServiceProvider serviceProvider, RectItemService rectItemService)
    {
        HostScreen = screen;
        _serviceProvider = serviceProvider;
        _rectItemService = rectItemService;
        _eventResults = new AvaloniaList<string>();

        LegendItems = new AvaloniaList<LegendItem>
        {
            new LegendItem { ClassName = "standing person", Color = "Green" },
            new LegendItem { ClassName = "fall person", Color = "Red" }
        };
    }
    #endregion

    #region Private Methods
    private void Render()
    {
        Log.Information("Start render event journal image");
        LogJournalViewModel.logString += "Start render event journal image\n";
        Log.Debug("EventJournalViewModel.Render: Start");
        LogJournalViewModel.logString += "EventJournalViewModel.Render: Start\n";
        var result = ParseSelectedVideoEventResult();
        var resultVideoInitRectItem = VideoInitRectItem(result);
        if (resultVideoInitRectItem is null)
        {
            return;
        }
        var (currentImage, title) = resultVideoInitRectItem.Value;
        CurrentImage = currentImage;
        Title = title.ToString();
        Log.Debug("EventJournalViewModel.Render: Done; Title: {@Title}; Event Result: {@VideoEventResult}", Title, result);
        LogJournalViewModel.logString += ("EventJournalViewModel.Render: Done; Title: {@Title}; Event Result: {@VideoEventResult}\n", Title, result);
        Log.Information("End render event journal image");
        LogJournalViewModel.logString += "End render event journal image\n";
    }

    private (Avalonia.Media.Imaging.Bitmap, Guid)? VideoInitRectItem(VideoEventResult videoEventResult)
    {
        Log.Debug("EventJournalViewModel.VideoInitRectItem: Start");
        LogJournalViewModel.logString += "EventJournalViewModel.VideoInitRectItem: Start\n";
        using ApplicationContext db = _serviceProvider.GetRequiredService<ApplicationContext>();

        var dbFrame = db.Frames.Where(frame => frame.FrameId == videoEventResult.Name).FirstOrDefault();

        if (dbFrame is null)
        {
            return null;
        }

        using var memoryStream = new MemoryStream(dbFrame.FrameData);
        Avalonia.Media.Imaging.Bitmap frameBitmap = new Avalonia.Media.Imaging.Bitmap(memoryStream);

        RectItem recognitionResult = new RectItem
        {
            X = videoEventResult.X,
            Y = videoEventResult.Y,
            Width = videoEventResult.Width,
            Height = videoEventResult.Height,
            Color = videoEventResult.Class switch
            {
                "standing person" => "Green",
                "fall person" => "Red"
            }
        };
        RectItems = [recognitionResult];

        return (frameBitmap, dbFrame.FrameId);
    }

    private VideoEventResult ParseSelectedVideoEventResult()
    {
        var values = SelectedEventResult.Split("; ");
        return new VideoEventResult
        {
            Name = Guid.Parse(values[0].Split(": ")[1]),
            Class = values[1].Split(": ")[1],
            X = Convert.ToInt32(values[2].Split(": ")[1]),
            Y = Convert.ToInt32(values[3].Split(": ")[1]),
            Width = Convert.ToInt32(values[4].Split(": ")[1]),
            Height = Convert.ToInt32(values[5].Split(": ")[1])
        };
    }

    private void Clear()
    {
        CurrentImage = null;
        Title = String.Empty;
        RectItems = null;
    }
    #endregion

    #region Private Video Methods
    private async Task DisplayVideoImages(VideoItemModel videoItem)
    {
        Log.Debug("EventJournalViewModel.DisplayVideoImages: Start");
        LogJournalViewModel.logString += "EventJournalViewModel.DisplayVideoImages: Start\n";
        EventResults = new();
        Guid videoGuid = videoItem.VideoId;

        using ApplicationContext db = _serviceProvider.GetRequiredService<ApplicationContext>();
        var dbVideo = db.Videos
            .Where((entity) => entity.VideoId == videoGuid)
            .Include((entity) => entity.Frames)
            .FirstOrDefault();

        if (dbVideo is null)
        {
            return;
        }

        List<Frame> dbFrames = dbVideo.Frames;

        List<Avalonia.Media.Imaging.Bitmap> framesBitmap = new List<Avalonia.Media.Imaging.Bitmap>();

        foreach (Frame dbFrame in dbFrames)
        {
            using var memoryStream = new MemoryStream(dbFrame.FrameData);
            framesBitmap.Add(new Avalonia.Media.Imaging.Bitmap(memoryStream));

            var currentDetections = await db.Detections.Where(d => d.FrameId == dbFrame.FrameId).ToListAsync();

            for (int idx = 0; idx < currentDetections.Count; idx++)
            {
                Detection det = currentDetections[idx];
                string eventLine = $"Name: {det.FrameId}; ClassName: {det.ClassName}; x: {det.X}; y: {det.Y}; width: {det.Width}; height: {det.Height}";
                EventResults.Add(eventLine);
            }
        }
    }
    #endregion

    #region Public Methods
    public async Task FillComboBox()
    {
        Log.Debug("VideoEventJournalViewModel.FillComboBox: Start");
        LogJournalViewModel.logString += "VideoEventJournalViewModel.FillComboBox: Start\n";
        using ApplicationContext db = _serviceProvider.GetRequiredService<ApplicationContext>();

        var videos = await db.Videos.Select((entity) => new VideoItemModel
        {
            VideoId = entity.VideoId,
            VideoName = entity.VideoName
        }).ToListAsync();

        VideoItems = videos;
        try
        {
            SelectedVideoItem = VideoItems[0];
        }
        catch (ArgumentOutOfRangeException)
        {
            ;
        }
    }

    public void ClearUI()
    {
        Log.Debug("VideoEventJournalViewModel.ClearUI: Start");
        LogJournalViewModel.logString += "VideoEventJournalViewModel.ClearUI: Start\n";
        Clear();
        try
        {
            VideoItems?.Clear();
        }
        catch (NullReferenceException ex)
        {
            ;
        }
        EventResults.Clear();
        SelectedVideoItem = null;
        Log.Debug("VideoEventJournalViewModel.ClearUI: End");
        LogJournalViewModel.logString += "VideoEventJournalViewModel.ClearUI: End\n";
    }
    #endregion

    #region Classes
    private class EventResult
    {
        public string Name { get; set; }

        public string Class { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }
    }

    private class VideoEventResult : EventResult
    {
        public Guid Name { get; set; }
    }

    public class VideoItemModel
    {
        public Guid VideoId { get; set; }

        public string VideoName { get; set; }

        public override string ToString()
        {
            return VideoName;
        }
    }
    #endregion
}
