using FluidBus.Core.BusProtocols;
using FluidBus.Core.Instructions;

namespace FluidBus.Core.Herits
{
	public abstract class FluidEvent : IFluidEvent
	{
		public string Id { get; }
		public BusProtocol Protocol { get; }
		public HashSet<IFluidInstruction> Instructions { get; }

		public FluidEvent(string id, BusProtocol protocol, params IFluidInstruction[] instructions)
		{
			Id = $"[EVT::{id}]";
			Protocol = protocol;
			Instructions = new(instructions);
		}

		public bool Dispatch(IFluidHandler handler)
			=> handler.Handle(this);
	}
}
