namespace FluidBus.Core.Errors
{
	public class ProtocolNotFoundException : FluidBusError
	{
		public ProtocolNotFoundException(string protocolName)
			: base($"[{nameof(ProtocolNotFoundException)}]: No port registered for protocol '{protocolName}'") { }
	}
}
