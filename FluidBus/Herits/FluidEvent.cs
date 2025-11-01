using FluidBus.Events;
using FluidBus.Handlers.Core;
using FluidBus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluidBus.Herits
{
	public abstract class FluidEvent : IFluidEvent
	{
		private DateTime?	execution;
		private string?		id;

		public	FluidEvent(string id)
			=> init(id);

		private void	init(string id)
		{
			this.execution = DateTime.UtcNow;
			this.id = id;
		}

		public void	Dispatch()
		{
			if (this is not BusLogEvent)
				Bus.Publish(new BusLogEvent($"[SYS] - Event Dispatched: {this.id}"));

			if (!HandlerLinq.TryResolve(this, out var handlers))
				return;

			var	registered = HandlerLinq.GetHandlers(handlers);
			if (registered != null)
			{
				foreach (var handler in registered)
				{
					if (handler.GetType().Equals(handlers.GetType()))
					{
						((IFluidHandler)handler).Handle((dynamic)this);
						if (this is not BusLogEvent)
							Bus.Publish(new BusLogEvent($"[SYS] - Handler Trigered: {((IFluidHandler)handler).GetId()}"));
					}
				}
			}

		}
	}
}
