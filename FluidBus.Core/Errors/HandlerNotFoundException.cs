namespace FluidBus.Core.Errors
{
	public class HandlerNotFoundException : FluidBusError
	{
		public HandlerNotFoundException(string eventId)
			: base($"[{nameof(HandlerNotFoundException)}]: No handler found for event '{eventId}'") { }
	}
}
