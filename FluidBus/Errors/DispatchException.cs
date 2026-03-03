using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Errors
{
	public class DispatchException : FluidBusError
	{
		public DispatchException(string  message) : base(message) { }
	}
}
