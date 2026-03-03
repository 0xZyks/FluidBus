using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Core.BusProtocols
{
	public enum ExecutionStrategy
	{
		Sync = 0,
		Async = 1,
	}

	public abstract class BusProtocol
	{
		public string Name { get; }
		public abstract ExecutionStrategy Strategy { get; }

		protected BusProtocol(string name)
			=> this.Name = name;

		public static readonly BusProtocol System = new SystemProtocol();
	}
}
