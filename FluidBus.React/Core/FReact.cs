using System.Diagnostics;
using FluidBus.React.Handlers;
using FluidBus.React.Interfaces;

namespace FluidBus.React.Core;

public class FReact
{
    private static Dictionary<Type, ReactChannel> channels = new();

    private static List<IReactHandler> handlers = new();

    static FReact()
    {
		new ReactLogHandler("react_logger");
    }

    public static bool RegisterHandler(IReactHandler hdl)
    {
        if (handlers.Contains(hdl))
            return false;
        handlers.Add(hdl);
        return true;
    }

    public static bool DropHandler(IReactHandler hdl)
	{
		if (!channels.TryGetValue(hdl.EventType, out var channel))
			return false;
		channels[hdl.EventType] = null!;
		return true;
	}

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
		var channel = GetOrCreateChannel(evt.GetType());
		channel.Write(evt);
        return true;
    }
}
