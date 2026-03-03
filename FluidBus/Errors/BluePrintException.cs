namespace FluidBus.Errors
{
	public class BluePrintException : FluidBusError
	{
		public BluePrintException(string msg) : base($"[{nameof(BluePrintException)}]: {msg}")
		{ }
	}
}
