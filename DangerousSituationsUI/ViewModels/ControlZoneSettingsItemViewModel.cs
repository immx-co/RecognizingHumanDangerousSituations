using DangerousSituationsUI.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace DangerousSituationsUI.ViewModels;

public class ControlZoneSettingsItemViewModel : ReactiveObject
{
    private ControlZone _controlZone;

    public ControlZone ControlZone
    {
        get => _controlZone;
        set => this.RaiseAndSetIfChanged(ref _controlZone, value);
    }
    
    public ReactiveCommand<Unit, Unit> DeleteCommand { get; set; }

    public event EventHandler Deleted;

    public ControlZoneSettingsItemViewModel(ControlZone controlZone)
    {
        _controlZone = controlZone;

        DeleteCommand = ReactiveCommand.Create(OnDelete);
    }

    public void OnDelete()
    {
        Deleted(this, new EventArgs());
    }
}
