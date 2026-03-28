using FluidBus.Core.Interfaces;
using FluidBus.React.Core;

namespace FluidBus.React.Abstracts;

public abstract class ReactEvent : IFluidEvent
{
    public string Id { get; }
    public HashSet<IFluidInstruction> Instructions { get; }

    public ReactEvent(string id, params IFluidInstruction[] instrs)
    {
        this.Id = id;
        this.Instructions = new(instrs);
    }
}
