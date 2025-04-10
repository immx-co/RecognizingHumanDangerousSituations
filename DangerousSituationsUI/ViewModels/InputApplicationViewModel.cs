using ReactiveUI;
using System;
using System.Threading;

namespace DangerousSituationsUI.ViewModels;

public class InputApplicationViewModel : ReactiveObject, IRoutableViewModel
{
    #region View Model Settings
    public IScreen HostScreen { get; }

    public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

    public CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    #endregion

    #region Contructor
    public InputApplicationViewModel(IScreen screen)
    {
        HostScreen = screen;
    }
    #endregion
}
