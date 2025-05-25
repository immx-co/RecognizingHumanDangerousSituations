using Avalonia.Collections;
using Avalonia.Media.Imaging;
using DangerousSituationsUI.Services;
using DangerousSituationsUI.Views;
using ReactiveUI;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;

namespace DangerousSituationsUI.ViewModels;

public class AddControlZoneDialogViewModel : ReactiveObject
{
    private Bitmap _frame;
    private List<ControlZone> _controlZones;
    private AvaloniaList<ZonePoint> _points;
    private ControlZone _controlZone;
    private double _frameWidth;
    private double _frameHeight;

    public ControlZone ControlZone
    {
        get => _controlZone;
        set => this.RaiseAndSetIfChanged(ref _controlZone, value);
    }

    public AvaloniaList<ZonePoint> Points
    {
        get => _points;
        set => this.RaiseAndSetIfChanged(ref _points, value);
    }

    public Bitmap Frame
    {
        get => _frame;
        set => this.RaiseAndSetIfChanged(ref _frame, value);
    }

    public double FrameWidth
    {
        get => _frameWidth;
        set => this.RaiseAndSetIfChanged(ref _frameWidth, value);
    }

    public double FrameHeight
    {
        get => _frameHeight;
        set => this.RaiseAndSetIfChanged(ref _frameHeight, value);
    }

    public List<ControlZone> ControlZones
    {
        get => _controlZones;
        set => this.RaiseAndSetIfChanged(ref _controlZones, value);
    }

    public AddControlZoneDialogWindow AddControlZoneDialogWindow { get; private set; }

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    public ReactiveCommand<Unit, Unit> ResetCommand { get; }

    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public AddControlZoneDialogViewModel(Bitmap frame, List<ControlZone> controlZones)
    {
        _frame = frame;

        var rectItem = new RectItemService().InitRect(
            (int)frame.Size.Width / 2,
            (int)frame.Size.Height / 2,
            frame.Size.Width,
            frame.Size.Height,
            frame);

        FrameHeight = rectItem.Height;
        FrameWidth = rectItem.Width;

        _controlZones = controlZones;

        OnReset();

        SaveCommand = ReactiveCommand.Create(OnSave);
        ResetCommand = ReactiveCommand.Create(OnReset);
        CancelCommand = ReactiveCommand.Create(OnCancel);

        AddControlZoneDialogWindow = new()
        {
            DataContext = this
        };
    }

    public void OnSave()
    {
        ControlZone.Points = Points.ToList();
        AddControlZoneDialogWindow.Close();
    }

    public void OnCancel()
    {
        AddControlZoneDialogWindow.Close();
    }

    public void OnReset()
    {
        ControlZone = new()
        {
            Description = string.Empty,
            Points = new()
        };
        Points = new();
    }
}
