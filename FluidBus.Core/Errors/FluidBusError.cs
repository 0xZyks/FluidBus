namespace FluidBus.Core.Errors
{
	public abstract class FluidBusError : Exception
	{
		public FluidBusError(string message) : base(message)
		{ }

		public FluidBusError(string message, Exception inner) : base(message, inner)
		{ }

		public void DisplayMessage()
			=> Console.WriteLine(this.Message);

	}
}
