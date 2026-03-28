using FluidBus.Core.Interfaces;
using FluidBus.React.Interfaces;

namespace FluidBus.React.Abstracts;

public abstract class ReactEvent : IReactEvent
{
    public string Id { get; }
    public List<IFluidInstruction> Instructions { get; }

    public ReactEvent(string id, params IFluidInstruction[] instrs)
    {
        this.Id = id;
        this.Instructions = new(instrs);
    }
}
