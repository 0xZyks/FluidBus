using FluidBus.Core.Herits;
using FluidBus.Event;

namespace FluidBus.Handler
{
    public class CoreHandler : FluidHandler<CoreEvent>
    {
        public CoreHandler(string id) : base($"HDL::{nameof(CoreHandler)}::{id}")
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
