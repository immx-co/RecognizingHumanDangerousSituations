using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DangerousSituationsUI;

public class IScreenRealization : ReactiveObject, IScreen
{
    public RoutingState Router { get; } = new RoutingState();
}
