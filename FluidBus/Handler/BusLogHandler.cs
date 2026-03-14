using FluidBus.Core.Herits;
using FluidBus.Event;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace FluidBus.Handler
{
	public class BusLogHandler : FluidHandler<BusLogEvent>
	{
		public BusLogHandler(string id) : base($"{nameof(BusLogEvent)}::{id}")
		{ }
	}
}
