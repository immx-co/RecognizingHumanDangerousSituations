using ReactiveUI;

namespace DangerousSituationsUI;

public class IScreenRealization : ReactiveObject, IScreen
{
    public RoutingState Router { get; } = new RoutingState();
}
