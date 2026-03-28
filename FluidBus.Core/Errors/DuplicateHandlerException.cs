namespace FluidBus.Core.Errors
{
	public class DuplicateHandlerException : FluidBusError
	{
		public DuplicateHandlerException(string handlerId)
			: base($"[{nameof(DuplicateHandlerException)}]: Handler '{handlerId}' is already registered") { }
	}
}
