using FluidBus.React.Interfaces;
using FluidBus.React.Core;
using FluidBus.Router.Interfaces;
using FluidBus.Router.Core;

namespace FluidBus;

public class FBus
{
    public static bool Route(IRouteEvent evt)
        => FRouter.Publish(evt);

    public static bool React(IReactEvent evt)
        => FReact.Publish(evt);
}
