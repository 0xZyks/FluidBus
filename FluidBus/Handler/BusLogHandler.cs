using FluidBus.Core.Herits;
using FluidBus.Event;

namespace FluidBus.Handler
{
	public class BusLogHandler : FluidHandler<BusLogEvent>
	{
		public BusLogHandler(string id) : base($"{nameof(BusLogEvent)}::{id}")
		{ }
	}
}
