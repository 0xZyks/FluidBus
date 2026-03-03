using FluidBus.Core.BusProtocols;
using FluidBus.Core.Instructions;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Core.Herits
{
	public interface IFluidEvent
	{
		string Id { get; }
		BusProtocol Protocol { get; }
		HashSet<IFluidInstruction> Instructions { get; }

		bool Dispatch(IFluidHandler hdl);
	}
}
