using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using ClassLibrary.Database.Models;
using ClassLibrary.Services;
using DangerousSituationsUI.Services;
using DangerousSituationsUI.Views;
using DynamicData;
using ReactiveUI;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace DangerousSituationsUI.ViewModels;

public class ControlZoneSettingsViewModel : ReactiveObject
{
    private Bitmap _frame;

    private double _frameWidth;

    private double _frameHeight;

    private AvaloniaList<ControlZone> _controlZones = new();

    private AvaloniaList<ControlZoneSettingsItem> _controlZonesItem = new();

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

    public Bitmap Frame
    {
        get => _frame;
        set => this.RaiseAndSetIfChanged(ref _frame, value);
    }

    public ControlZoneSettingsWindow ControlZoneSettingsWindow { get; }

    public AvaloniaList<ControlZone> ControlZones
    {
        get => _controlZones;
        set => this.RaiseAndSetIfChanged(ref _controlZones, value);
    }

    public AvaloniaList<ControlZoneSettingsItem> ControlZonesItems
    {
        get => _controlZonesItem;
        set => this.RaiseAndSetIfChanged(ref _controlZonesItem, value);
    }

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    public ReactiveCommand<Unit, Task> AddCommand { get; }

    public ControlZoneSettingsViewModel(Bitmap frame)
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

        SaveCommand = ReactiveCommand.Create(OnSave);
        AddCommand = ReactiveCommand.Create(OnAddAsync);

        ControlZoneSettingsWindow = new ControlZoneSettingsWindow
        {
            DataContext = this
        };
    }

    public void OnSave()
    {
        ControlZoneSettingsWindow.Close();
    }

    public async Task OnAddAsync()
    {
        var AddControlZoneDialogViewModel = new AddControlZoneDialogViewModel(_frame, _controlZones.ToList());
        
        await AddControlZoneDialogViewModel.AddControlZoneDialogWindow.ShowDialog(ControlZoneSettingsWindow);

        ControlZones.Add(AddControlZoneDialogViewModel.ControlZone);

        var controlZoneSettingsItemViewModel = new ControlZoneSettingsItemViewModel(AddControlZoneDialogViewModel.ControlZone);


        ControlZonesItems.Add(new ControlZoneSettingsItem { DataContext = controlZoneSettingsItemViewModel});
    }
}
