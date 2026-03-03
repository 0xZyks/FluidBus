using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Core.BusProtocols
{
	public class SystemProtocol : BusProtocol
	{
		public override ExecutionStrategy Strategy => ExecutionStrategy.Sync;
		public SystemProtocol() : base ("SYSTEM") 
		{ }
	}
}
