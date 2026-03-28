using FluidBus.React.Interfaces;
using FluidBus.React.Core;
using FluidBus.Router.Interfaces;
using FluidBus.Router.Core;

namespace FluidBus;

public class FBus
{
    public static bool Route<T>(T evt) where T : IRouteEvent
        => FRouter.Publish(evt);

    public static bool React<T>(T evt) where T : IReactEvent
        => FReact.Publish(evt);
}
