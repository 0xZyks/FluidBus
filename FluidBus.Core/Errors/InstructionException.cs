namespace FluidBus.Core.Errors
{
	public class InstructionException : FluidBusError
	{
		public InstructionException(string message)
			: base($"[{nameof(InstructionException)}]: {message}") { }

		public InstructionException(string message, Exception inner)
			: base($"[{nameof(InstructionException)}]: {message}") { }
	}
}
