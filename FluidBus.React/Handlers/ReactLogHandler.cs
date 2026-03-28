using FluidBus.React.Abstracts;
using FluidBus.React.Events;

namespace FluidBus.React.Handlers
{
	public class ReactLogHandler : ReactHandler<ReactLogEvent>
	{
		public ReactLogHandler(string id) : base($"{nameof(ReactLogEvent)}::{id}")
		{ }
	}
}
