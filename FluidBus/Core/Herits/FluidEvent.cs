using FluidBus.BluePrint;
using FluidBus.Core.BusProtocols;
using FluidBus.Core.HLinq;
using FluidBus.Core.Instructions;
using FluidBus.Core.Instructions.System;
using FluidBus.Event;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace FluidBus.Core.Herits
{
	public abstract class FluidEvent : IFluidEvent
	{
		public string Id { get; }
		public BusProtocol Protocol { get; }
		public HashSet<IFluidInstruction> Instructions { get; }

		public FluidEvent(string id, BusProtocol protocol, params IFluidInstruction[] instructions)
		{
			Id = id;
			Protocol = protocol;
			Instructions = new(instructions);
		}

		public bool Dispatch(IFluidHandler handler)
		{
			if (this is not BusLogEvent)
			{
				FBus.Publish(createBusLogEvent("bus_log_dispatch", $"[SYS] - Event Dispatched: {this.Id}"));
			}
			
			handler.Handle(this);
			if (this is not BusLogEvent)
				FBus.Publish(createBusLogEvent("bus_log_dispatch", $"[SYS] - Handler Trigered: {handler.Id}"));
			return true;
		}

		private IFluidEvent createBusLogEvent(string name, string message)
		{
			var (busEvt, success) = BluePrintFactory
					.NewEvent(
						typeof(BusLogEvent),
						name,
						BusProtocol.System,
						new LogInstruction(message, msg => Console.WriteLine(msg))
					);
			if (success)
				return busEvt;
			return null!;
		}
	}
}
