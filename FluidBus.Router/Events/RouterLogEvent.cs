using FluidBus.Core.Interfaces;
using FluidBus.Core.Protocols;
using FluidBus.Router.Abstracts;

namespace FluidBus.Router.Events
{
	public class RouterLogEvent : RouteEvent
	{
		public RouterLogEvent(string id, BusProtocol protocol, params IFluidInstruction[] instrs) : base ($"{nameof(RouterLogEvent)}::{id}", protocol, instrs)
		{

		}
	}
}
