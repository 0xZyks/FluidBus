using FluidBus.Core.Errors;
using FluidBus.Core.Interfaces;
using FluidBus.Core.Tasks;
using FluidBus.React.Interfaces;
using System.Threading.Channels;

namespace FluidBus.React.Core;

public delegate void ReactReceive(IFluidEvent evt);

public class ReactChannel
{
	private readonly List<ReactReceive> _subscribers = new();
	private readonly Channel<IReactEvent> _queue
		= Channel.CreateUnbounded<IReactEvent>();
	public string Name { get; }

	public ReactChannel(string name)
	{
		this.Name = name;
		new FluidTask(async () =>
		{
			await foreach (var evt in _queue.Reader.ReadAllAsync())
				foreach (var sub in _subscribers)
					sub(evt);
		});
	}

	public void OnReceive(ReactReceive callback)
		=> _subscribers.Add(callback);

	public void Write(IReactEvent evt)
	{
		if (_subscribers.Count == 0)
			throw new ChannelException(Name, "No subscribers on this channel");
		_queue.Writer.TryWrite(evt);
	}
}
