using FluidBus.Core.Interfaces;
using FluidBus.Core.Protocols;
using FluidBus.Core.Errors;
using FluidBus.Router.Abstracts;
using FluidBus.Router.Interfaces;
using FluidBus.Router.HLinq;
using FluidBus.Router.Handlers;

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

		public static bool Register(IRouteHandler hdl)
			=> HandlerLinq.Register(hdl);

		public static bool TryGetHandler(IRouteEvent evt, out IFluidHandler handler)
			=> HandlerLinq.TryGetHandler(evt, out handler);

		public static bool Publish(IRouteEvent evt)
		{
            if (!_ports.TryGetValue(evt.Protocol, out var port))
                throw new ProtocolNotFoundException(evt.Protocol.Name);
            if (!HandlerLinq.TryGetHandler(evt, out var handler))
                throw new HandlerNotFoundException(evt.Id);
            return port.Dispatch(evt, handler);
		}
	}
}
