using FluidBus.Core.Interfaces;
using FluidBus.Core.Errors;

namespace FluidBus.Router.HLinq
{
	public static class HandlerLinq
	{
		private static Dictionary<Type, IFluidHandler> handlers = new();

		public static bool Register(IFluidHandler handler)
		{
            if (handlers.ContainsKey(handler.EventType))
                throw new DuplicateHandlerException(handler.Id);
            handlers[handler.EventType] = handler;
            return true;
		}

		public static bool Drop(IFluidHandler handler)
		{
            if (handlers.Remove(handler.EventType))
                return true;
			throw new HandlerNotFoundException(handler.Id);
		}

		public static bool TryGetHandler(IFluidEvent evt, out IFluidHandler hdl)
            => handlers.TryGetValue(evt.GetType(), out hdl!);
	}
}
