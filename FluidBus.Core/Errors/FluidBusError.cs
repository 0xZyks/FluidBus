namespace FluidBus.Core.Errors
{
	public abstract class FluidBusError : Exception
	{
		public FluidBusError(string message) : base(message)
		{ }

		public void DisplayMessage()
			=> Console.WriteLine(this.Message);

	}
}
