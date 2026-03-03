using FluidBus.Core.BusProtocols;
using FluidBus.Core.Herits;
using FluidBus.Core.Instructions;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Event
{
	public class BusLogEvent : FluidEvent
	{
		public BusLogEvent(string id, BusProtocol protocol, params IFluidInstruction[] instrs) : base ($"[EVT::{nameof(BusLogEvent)}::{id}]", protocol, instrs)
		{

		}
	}
}
