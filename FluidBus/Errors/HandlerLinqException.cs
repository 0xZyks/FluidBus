using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Errors
{
	public class HandlerLinqException : FluidBusError
	{
		public HandlerLinqException(string msg) : base($"[{nameof(HandlerLinqException)}]: {msg}")
		{ }
	}
}
