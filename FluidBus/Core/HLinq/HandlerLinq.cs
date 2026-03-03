using FluidBus.Core.Herits;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Core.HLinq
{
	public static class HandlerLinq
	{
		private static Dictionary<Type, Dictionary<IFluidHandler, bool>> handlers = new();

		public static bool Register(IFluidHandler handler)
		{
			if (!handlers.ContainsKey(handler.EventType))
				handlers[handler.EventType] = new();
			handlers[handler.EventType].Add(handler, false);
			return true;
		}

		public static bool Drop(IFluidHandler handler)
		{
			if (handlers.ContainsKey(handler.EventType))
				return handlers[handler.EventType].Remove(handler);
			return false;
		}

		public static bool TryGetHandlers(IFluidEvent evt, out Dictionary<IFluidHandler, bool> hdls)
		{
			foreach (var target in handlers.Keys)
				if (evt.GetType() == target)
				{ hdls = handlers[target]; return true; }
			hdls = null!;
			return false;
		}
	}
}
