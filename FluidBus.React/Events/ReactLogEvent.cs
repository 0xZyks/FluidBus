using FluidBus.Core.Interfaces;
using FluidBus.React.Abstracts;

namespace FluidBus.React.Events
{
	public class ReactLogEvent : ReactEvent
	{
		public ReactLogEvent(string id, params IFluidInstruction[] instrs) : base($"{nameof(ReactLogEvent)}::{id}", instrs)
		{

		}
	}
}
