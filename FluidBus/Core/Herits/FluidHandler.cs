using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.Core.Herits
{
	public abstract class FluidHandler<T> : IFluidHandler
	{
		public string Id { get; }
		public int CallCount { get; set; }

		public Type EventType { get; }

		public FluidHandler(string id)
		{
			this.Id = id;
			this.EventType = typeof(T);
		}


		public abstract bool Handle(IFluidEvent evt);
	}
}
