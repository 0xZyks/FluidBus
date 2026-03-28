using FluidBus.Core.Protocols;

namespace FluidBus.Core.Interfaces
{
	public interface IFluidEvent
	{
		string Id { get; }
		BusProtocol Protocol { get; }
		HashSet<IFluidInstruction> Instructions { get; }

		bool Dispatch(IFluidHandler hdl);
	}
}
