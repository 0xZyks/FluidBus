using FluidBus.Core.Interfaces;
using FluidBus.Core.Tasks;
using FluidBus.Core.Errors;
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
    {
        if (this._subscribers.Count == 0)
            throw new ChannelException(Name, "No subscribers on this channel");
        this._subscribers.ForEach(callback =>
            new FluidTask(() => callback(evt))
                .OnComplete(state =>
                {
                    if (state == FluidTaskState.Failed)
                        new ChannelException(Name, $"Callback failed for event '{evt.Id}'").DisplayMessage();
                }));
    }
}
