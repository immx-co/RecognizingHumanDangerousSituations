using Avalonia.Media.Imaging;
using ClassLibrary.Database.Models;
using ClassLibrary.Services;
using DangerousSituationsUI.Services;
using DangerousSituationsUI.Views;
using ReactiveUI;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading;
using Telegram.Bot.Types;

namespace DangerousSituationsUI.ViewModels;

public class ControlZoneSettingsViewModel : ReactiveObject
{
    private Bitmap _frame;
    private int _box_tl_x;
    private int _box_tl_y;
    private int _box_width;
    private int _box_height;
    private bool _boxPositionChanged;

    public Bitmap Frame
    {
        get => _frame;
        set => this.RaiseAndSetIfChanged(ref _frame, value);
    }

    public ControlZoneSettingsWindow ControlZoneSettingsWindow { get; }

    public List<RectItem> ControlZones { get; private set; }

    public int BoxTopLeftX
    {
        get => _box_tl_x;
        set => this.RaiseAndSetIfChanged(ref _box_tl_x, value);
    }

    public int BoxTopLeftY
    {
        get => _box_tl_y;
        set => this.RaiseAndSetIfChanged(ref _box_tl_y, value);
    }

    public int BoxWidth
    {
        get => _box_width;
        set => this.RaiseAndSetIfChanged(ref _box_width, value);
    }

    public int BoxHeight
    {
        get => _box_height;
        set => this.RaiseAndSetIfChanged(ref _box_height, value);
    }

    public bool BoxPositionChanged
    {
        get => _boxPositionChanged;
        set => this.RaiseAndSetIfChanged(ref _boxPositionChanged, value);
    }

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    public ControlZoneSettingsViewModel(Bitmap frame)
    {
        _frame = frame;

        SaveCommand = ReactiveCommand.Create(OnSave);

        ControlZones = [new RectItemService().InitRect(
            (int)frame.Size.Width / 2, 
            (int)frame.Size.Height / 2,
            frame.Size.Width, frame.Size.Height, 
            frame)];

        BoxTopLeftX = ControlZones.First().X;
        BoxTopLeftY = ControlZones.First().Y;
        BoxHeight = ControlZones.First().Height;
        BoxWidth = ControlZones.First().Width;

        ControlZoneSettingsWindow = new ControlZoneSettingsWindow
        {
            DataContext = this
        };
    }

    public void OnSave()
    {
        

        ControlZoneSettingsWindow.Close();
    }
}
