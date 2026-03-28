using FluidBus.Core.Protocols;

namespace FluidBus.Core.Interfaces
{
	public interface IFluidEvent
	{
		string Id { get; }
		HashSet<IFluidInstruction> Instructions { get; }
	}
}
