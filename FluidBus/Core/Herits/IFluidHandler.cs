using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Core.Herits
{
	public interface IFluidHandler
	{
		string Id { get; }
		int CallCount { get; set; }

		Type EventType { get; }

		abstract bool Handle(IFluidEvent evt);
	}
}
