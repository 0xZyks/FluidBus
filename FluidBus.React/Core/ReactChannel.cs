using FluidBus.Core.Interfaces;
using FluidBus.Core.Tasks;
using FluidBus.React.Abstracts;

namespace FluidBus.React.Core;

public delegate void ReactReceive(IFluidEvent evt);

public class ReactChannel
{
    private List<ReactReceive> _subscribers;
    public string Name { get; }

    public ReactChannel(string name)
    {
        this._subscribers = new();
        this.Name = name;
    }

    public void OnReceive(ReactReceive callback)
        => this._subscribers.Add(callback);

    public void Write(ReactEvent evt)
        => this._subscribers.ForEach(callback => new FluidTask(() => callback(evt)));
}
