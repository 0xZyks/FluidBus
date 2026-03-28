using FluidBus.React.Abstracts;

namespace FluidBus.React.Core;

public class FReact
{
    private static Dictionary<Type, ReactChannel> _channels = new();

    public static ReactChannel GetOrCreateChannel(Type listen)
    {
        if (_channels.TryGetValue(listen, out var channel))
            return channel;
        channel = new ReactChannel(listen.Name);
        _channels[listen] = channel;
        return null!;
    }

    public static bool Publish(ReactEvent evt)
    {
        GetOrCreateChannel(evt.GetType()).Write(evt);
        return true;
    }
}
