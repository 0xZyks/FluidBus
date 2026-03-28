using FluidBus.React.Interfaces;

namespace FluidBus.React.Core;

public class FReact
{
    private static Dictionary<Type, ReactChannel> channels = new();

    private static List<IReactHandler> handlers = new();

    public static bool RegisterHandler(IReactHandler hdl)
    {
        if (handlers.Contains(hdl))
            return false;
        handlers.Add(hdl);
        return true;
    }

    public static bool DropHandler(IReactHandler hdl)
        => handlers.Remove(hdl);

    public static ReactChannel GetOrCreateChannel(Type listen)
    {
        if (channels.TryGetValue(listen, out var channel))
            return channel;
        channel = new ReactChannel(listen.Name);
        channels[listen] = channel;
        return channel;
    }

    public static bool Publish(IReactEvent evt)
    {
        GetOrCreateChannel(evt.GetType()).Write(evt);
        return true;
    }
}
