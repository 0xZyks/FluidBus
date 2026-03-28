using FluidBus.Core.Interfaces;
using FluidBus.Core.Protocols;

namespace FluidBus.Router.Interfaces
{
	public interface IRouteEvent : IFluidEvent
	{
		BusProtocol Protocol { get; }
		bool Dispatch(IFluidHandler handler);
	}
}
