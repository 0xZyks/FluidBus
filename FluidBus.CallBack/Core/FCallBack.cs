using FluidBus.Core.Abstracts;

namespace FluidBus.CallBack.Core;

public class FCallBack
{
    private static Dictionary<string, FluidCallBack> callbacks = new();

    public static bool RegisterCallBack(string name, FluidCallBack callback)
    {
        if (callbacks.TryGetValue(name, out var cb))
            return false;
		callbacks[name] = callback;
        return true;
    }

	public static bool DropCallBack(string name)
	{
		if (callbacks.TryGetValue(name, out var _))
			return callbacks.Remove(name);
		return false;
	}

	private static FluidCallBack? GetCallBack(string name)
	{
		if (!callbacks.TryGetValue(name, out var cb))
			return null;
		return cb;
	}

	public static object? Execute(string name, object? data)
	{
		var callback = GetCallBack(name);
		if (callback == null)
			return null;
		return callback.Invoke(data);
	}
}
