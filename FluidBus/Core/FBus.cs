using FluidBus.Core.BusProtocols;
using FluidBus.Core.Herits;
using FluidBus.Core.HLinq;
using FluidBus.Core.Tasks;
using FluidBus.Errors;
using FluidBus.Handler;

namespace FluidBus.Core
{
	public static class FBus
	{
		private static Dictionary<BusProtocol, BusPort> _ports;
		static FBus()
		{
			_ports = new();
			HandlerLinq.Register(new BusLogHandler("bus_logger"));
			_ports[BusProtocol.System] = new BusPort(BusProtocol.System);
		}

		public static void AddPort(BusProtocol protocol)
			=> _ports[protocol] = new BusPort(protocol);

		public static bool Register(IFluidHandler hdl)
			=> HandlerLinq.Register(hdl);

		public static bool TryGetHandlers(IFluidEvent evt, out List<IFluidHandler> handlers)
			=> HandlerLinq.TryGetHandlers(evt, out handlers);

		public static bool Publish(IFluidEvent evt)
		{
            if (!_ports.TryGetValue(evt.Protocol, out var port))
                return false;
            if (!HandlerLinq.TryGetHandlers(evt, out var handlers))
                return false;
            return port.Dispatch(evt, handlers.First());
		}
	}

	internal class BusPort
	{
		public BusProtocol Protocol { get; }
		public BusPort(BusProtocol protocol)
			=> this.Protocol = protocol;

		public bool Dispatch(IFluidEvent evt, IFluidHandler hdl)
		{
			switch (this.Protocol.Strategy)
			{
				case ExecutionStrategy.Sync:
					return evt.Dispatch(hdl);
				case ExecutionStrategy.Async:
					new FluidTask(() => evt.Dispatch(hdl))
						.OnComplete(state =>
						{
							if (state == FluidTaskState.Failed)
								new DispatchException("Dispatch failed").DisplayMessage();
						});
					return true;
				default:
					return false;
			}
		}

		public override bool Equals(object? obj)
		{
			if (obj is BusPort other)
				return Protocol.Name.Equals(other.Protocol.Name);
			return false;
		}

		public override int GetHashCode()
			=> HashCode.Combine(Protocol.Name);
	}
}
