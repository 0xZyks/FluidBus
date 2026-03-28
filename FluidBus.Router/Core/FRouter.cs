using FluidBus.Core.Interfaces;
using FluidBus.Core.Protocols;
using FluidBus.Core.Errors;
using FluidBus.Router.HLinq;
using FluidBus.Router.Handlers;
using FluidBus.Router.Abstracts;

namespace FluidBus.Router.Core
{
	public static class FRouter
	{
		private static Dictionary<BusProtocol, RouterPort> _ports;
		static FRouter()
		{
			_ports = new();
			HandlerLinq.Register(new RouterLogHandler("bus_logger"));
			_ports[BusProtocol.System] = new RouterPort(BusProtocol.System);
		}

		public static void AddPort(BusProtocol protocol)
			=> _ports[protocol] = new RouterPort(protocol);

		public static bool Register(IFluidHandler hdl)
			=> HandlerLinq.Register(hdl);

		public static bool TryGetHandlers(IFluidEvent evt, out List<IFluidHandler> handlers)
			=> HandlerLinq.TryGetHandlers(evt, out handlers);

		public static bool Publish(RouteEvent evt)
		{
            if (!_ports.TryGetValue(evt.Protocol, out var port))
                throw new ProtocolNotFoundException(evt.Protocol.Name);
            if (!HandlerLinq.TryGetHandlers(evt, out var handlers))
                throw new HandlerNotFoundException(evt.Id);
            return port.Dispatch(evt, handlers.First());
		}
	}
}
