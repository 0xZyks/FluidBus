using FluidBus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluidBus.Herits
{
	public abstract class FluidHandler<T> : IFluidHandler
	{
		private string?	_id;

		public	FluidHandler(string id)
			=> this.init(id);

		private void	init(string id)
		{
			this._id = id;
			// Maybe smth else ? :/
		}

		public string	GetId()
			=> this._id!;

		public abstract void	Handle(T evt);

		void IFluidHandler.Handle(IFluidEvent evt)
			=> this.Handle((T)evt);
	}
}
