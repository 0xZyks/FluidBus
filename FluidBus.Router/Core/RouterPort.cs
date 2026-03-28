using FluidBus.Core.Interfaces;
using FluidBus.Core.Protocols;
using FluidBus.Core.Tasks;
using FluidBus.Core.Errors;

namespace FluidBus.Router.Core
{
	internal class RouterPort
	{
		public BusProtocol Protocol { get; }
		public RouterPort(BusProtocol protocol)
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
			if (obj is RouterPort other)
				return Protocol.Name.Equals(other.Protocol.Name);
			return false;
		}

		public override int GetHashCode()
			=> HashCode.Combine(Protocol.Name);
	}
}
