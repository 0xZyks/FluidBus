using FluidBus.Core.Interfaces;
using FluidBus.React.Abstracts;
using FluidBus.React.Core;

namespace FluidBus.React.Abstracts;

public abstract class ReactHandler<T> : IFluidHandler
{
    public int CallCount { get; set; }
    public string Id { get; }
    public Type EventType { get; }

    public ReactHandler(string id)
    {
        this.Id = id;
        this.EventType = typeof(T);
        this.Subscribe();
    }

    private void Subscribe()
    {
        FReact.GetOrCreateChannel(this.EventType)
            .OnReceive(evt => this.Handle(evt));
    }

    public virtual bool Handle(IFluidEvent evt)
    {
        foreach (var instr in evt.Instructions)
        {
            if (instr.HasMethod)
                instr.Execute();
            if (instr.HasFuncs)
                instr.ExecuteAndGet();
        }
        this.CallCount++;
        return true;
    }
}
