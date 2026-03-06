using FluidBus.Core.BusProtocols;
using FluidBus.Core.Herits;
using FluidBus.Core.Instructions;

namespace FluidBus.Event
{
    public class CoreEvent : FluidEvent
    {
        public CoreEvent(string id, BusProtocol protocol, params IFluidInstruction[] instrs) : base($"[EVT::{nameof(CoreEvent)}::{id}]", protocol, instrs)
        {

        }
    }
}
