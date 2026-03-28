namespace FluidBus.Core.Errors
{
	public class HandlerLinqException : FluidBusError
	{
		public HandlerLinqException(string msg) : base($"[{nameof(HandlerLinqException)}]: {msg}")
		{ }
	}
}
