using Avalonia.Collections;
using ClassLibrary.Database.Models;
using ClassLibrary.Services;
using DangerousSituationsUI.Services;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DangerousSituationsUI.ViewModels;

public class EventJournalViewModel : ReactiveObject, IRoutableViewModel
{
    #region Private Fields
    private Dictionary<string, AvaloniaList<string>> _eventResults;

    private AvaloniaList<string> _currentEventResults = new();

    private AvaloniaList<string> _imageNames = new();

    private string? _selectedImageName;

    private AvaloniaList<RectItem> _rectItems = new();

    private Avalonia.Media.Imaging.Bitmap _currentImage;

    private Dictionary<string, Avalonia.Media.Imaging.Bitmap> _imagesDictionary = new();

    private RectItemService _rectItemService;

    private string _selectedEventResult;

    private AvaloniaList<LegendItem> _legendItems;
    #endregion

    #region View Model Settings
    public IScreen HostScreen { get; }
    public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

    public CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    #endregion

    #region Properties
    public Dictionary<string, AvaloniaList<string>> EventResults
    {
        get => _eventResults;
        set => this.RaiseAndSetIfChanged(ref _eventResults, value);
    }

    public AvaloniaList<string> CurrentEventResults
    {
        get => _currentEventResults;
        set => this.RaiseAndSetIfChanged(ref _currentEventResults, value);
    }

    public AvaloniaList<string> ImageNames
    {
        get => _imageNames;
        set => this.RaiseAndSetIfChanged(ref _imageNames, value);
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

    public Dictionary<string, Avalonia.Media.Imaging.Bitmap> ImagesDictionary
    {
        get => _imagesDictionary;
        set => this.RaiseAndSetIfChanged(ref _imagesDictionary, value);
    }

    public string SelectedImageName
    {
        get => _selectedImageName;
        set
        {
            if (value != string.Empty && value != null) CurrentEventResults = EventResults[value];
            this.RaiseAndSetIfChanged(ref _selectedImageName, value);
        }
    }

    public AvaloniaList<LegendItem> LegendItems
    {
        get => _legendItems;
        set => this.RaiseAndSetIfChanged(ref _legendItems, value);
    }
    #endregion

    #region Constructor
    public EventJournalViewModel(IScreen screen, RectItemService rectItemService)
    {
        HostScreen = screen;
        _rectItemService = rectItemService;
        _eventResults = new Dictionary<string, AvaloniaList<string>>();

        LegendItems = new AvaloniaList<LegendItem>
        {
            new LegendItem { ClassName = "human", Color = "Green" },
            new LegendItem { ClassName = "wind/sup-board", Color = "Red" },
            new LegendItem { ClassName = "bouy", Color = "Blue" },
            new LegendItem { ClassName = "sailboat", Color = "Yellow" },
            new LegendItem { ClassName = "kayak", Color = "Purple" }
        };
    }
    #endregion

    #region Private Methods
    private void Render()
    {
        Log.Information("Start render event journal image");
        Log.Debug("EventJournalViewModel.Render: Start");
        var result = ParseSelectedEventResult();
        InitRectItem(result);
        CurrentImage = ImagesDictionary[SelectedImageName];
        Log.Information("End render event journal image");
        Log.Debug("EventJournalViewModel.Render: Done; Image Name: {@ImageName}; Event Result: {@EventResult}", SelectedImageName, result);
    }

    private void InitRectItem(EventResult eventResult)
    {
        Log.Debug("EventJournalViewModel.InitRectItem: Start");
        RecognitionResult recognitionResult = new RecognitionResult()
        {
            ClassName = eventResult.Class,
            X = eventResult.X,
            Y = eventResult.Y,
            Width = eventResult.Width,
            Height = eventResult.Height
        };

        RectItems = [_rectItemService.InitRect(recognitionResult, ImagesDictionary[SelectedImageName])];
        Log.Debug("EventJournalViewModel.InitRectItem: Done; Recognition Result: {@RecognitionResult}", recognitionResult);
    }

    private EventResult ParseSelectedEventResult()
    {
        var values = SelectedEventResult.Split("; ");
        return new EventResult
        {
            Class = values[0].Split(": ")[1],
            X = Convert.ToInt32(values[1].Split(": ")[1]),
            Y = Convert.ToInt32(values[2].Split(": ")[1]),
            Width = Convert.ToInt32(values[3].Split(": ")[1]),
            Height = Convert.ToInt32(values[4].Split(": ")[1])
        };
    }

    private void Clear()
    {
        CurrentImage = null;
        SelectedImageName = String.Empty;
        RectItems = null;
    }

    #endregion

    private class EventResult
    {
        public string Class { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }
    }
}
