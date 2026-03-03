using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Errors
{
	public abstract class FluidBusError : Exception
	{
		public FluidBusError(string message) : base(message)
		{ }

		public void DisplayMessage()
			=> Console.WriteLine(this.Message);

	}
}
