using FluidBus.Core.Interfaces;
using FluidBus.Core.Protocols;
using FluidBus.Router.Interfaces;

namespace FluidBus.Router.Abstracts
{
	public abstract class RouteEvent : IRouteEvent
	{
		public string Id { get; }
		public BusProtocol Protocol { get; }
		public List<IFluidInstruction> Instructions { get; }

		public RouteEvent(string id, BusProtocol protocol, params IFluidInstruction[] instructions)
		{
			Id = $"[EVT::{id}]";
			Protocol = protocol;
			Instructions = new(instructions);
		}

		public bool Dispatch(IFluidHandler handler)
			=> handler.Handle(this);
	}
}
