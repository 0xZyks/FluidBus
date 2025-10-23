using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluidBus.Herits
{
	public interface IFluidHandler
	{
		void	Handle(IFluidEvent evt);

		string	GetId();
	}
}
