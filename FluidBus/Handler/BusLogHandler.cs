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
		public BusLogHandler(string id) : base($"[HDL::{nameof(BusLogEvent)}::{id}]")
		{ }

		public override bool Handle(IFluidEvent evt)
		{
			foreach (var instr in evt.Instructions)
			{
				instr.Execute();
				instr.ExecuteAndGet();
			}
			this.CallCount++;
			return true;
		}
	}
}
