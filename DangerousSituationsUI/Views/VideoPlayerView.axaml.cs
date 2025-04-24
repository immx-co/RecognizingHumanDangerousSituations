using Avalonia.ReactiveUI;
using DangerousSituationsUI.ViewModels;

namespace DangerousSituationsUI.Views;

public partial class VideoPlayerView : ReactiveUserControl<VideoPlayerViewModel>
{
    public VideoPlayerView()
    {
        InitializeComponent();
    }
}