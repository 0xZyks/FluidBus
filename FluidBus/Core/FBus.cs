using FluidBus.BluePrint;
using FluidBus.Core.BusProtocols;
using FluidBus.Core.Herits;
using FluidBus.Core.HLinq;
using FluidBus.Core.Tasks;
using FluidBus.Errors;
using FluidBus.Event;
using FluidBus.Handler;
using System.Runtime.CompilerServices;

namespace FluidBus.Core
{
	public static class FBus
	{
		private static HashSet<BusPort> _ports;
		static FBus()
		{
            FluidCoreAPI.Initialize();
			_ports = new();
			HandlerLinq.Register(new BusLogHandler("bus_logger"));
			_ports.Add(new BusPort(BusProtocol.System));
		}

		public static bool AddPort(BusProtocol protocol)
			=> _ports.Add(new BusPort(protocol));

		public static bool Register(IFluidHandler hdl)
			=> HandlerLinq.Register(hdl);

		public static bool TryGetHandlers(IFluidEvent evt, out Dictionary<IFluidHandler, bool> handlers)
			=> HandlerLinq.TryGetHandlers(evt, out handlers);

		public static bool Publish(IFluidEvent evt)
		{
			IFluidHandler? available = null;
			foreach (var port in _ports)
			{
				if (!evt.Protocol.Equals(port.Protocol))
					continue;
				if (!HandlerLinq.TryGetHandlers(evt, out var handlers))
					return false;
				foreach (var handler in handlers)
				{
					if (!handler.Value)
					{ available = handler.Key; break; }
				}
				if (available != null)
				{ available.CallCount++; return port.Dispatch(evt, available); }
				var (hdl, success) = BluePrintFactory.NewHandler(handlers.Last().Key);
				hdl.CallCount++;
				return success && port.Dispatch(evt, hdl);
			}
			return false;
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
