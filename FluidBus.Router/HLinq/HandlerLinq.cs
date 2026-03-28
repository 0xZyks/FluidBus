using FluidBus.Core.Interfaces;

namespace FluidBus.Router.HLinq
{
	public static class HandlerLinq
	{
		private static Dictionary<Type, List<IFluidHandler>> handlers = new();

		public static bool Register(IFluidHandler handler)
		{
            if (!handlers.TryGetValue(handler.EventType, out var list))
            { list = new(); handlers[handler.EventType] = list; }
            list.Add(handler);
            return true;
		}

		public static bool Drop(IFluidHandler handler)
		{
            if (handlers.TryGetValue(handler.EventType, out var list))
                return list.Remove(handler);
			return false;
		}

		public static bool TryGetHandlers(IFluidEvent evt, out List<IFluidHandler> hdls)
            => handlers.TryGetValue(evt.GetType(), out hdls!);
	}
}
