using FluidBus.Core.Interfaces;
using FluidBus.Router.Interfaces;

namespace FluidBus.Router.Abstracts
{
	public abstract class RouteHandler<T> : IRouteHandler
	{
		public string Id { get; }
		public int CallCount { get; set; }

		public Type EventType { get; }

		public RouteHandler(string id)
		{
			this.Id = $"[HDL::{id}]";
			this.EventType = typeof(T);
		}


		public virtual bool Handle(IFluidEvent evt)
		{
			foreach (var instr in evt.Instructions)
                if (instr.HasMethod)
				    instr.Execute();
			this.CallCount++;
			return true;
		}
	}
}
