using FluidBus.Router.Abstracts;
using FluidBus.Router.Events;

namespace FluidBus.Router.Handlers
{
	public class RouterLogHandler : RouteHandler<RouterLogEvent>
	{
		public RouterLogHandler(string id) : base($"{nameof(RouterLogEvent)}::{id}")
		{ }
	}
}
