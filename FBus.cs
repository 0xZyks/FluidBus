using FluidBus.Core;
using FluidBus.React;
using FluidBus.Router.Abstracts;
using FluidBus.Router.Core;

namespace FluidBus;

public class FBus
{
    public static bool Route<T>(T evt) where T : RouteEvent
        => FRouter.Publish(evt);

    public static bool React<T>(T evt) where T : ReactEvent
        => FReact.Publish(evt);
}
