using FluidBus.Events;
using FluidBus.Herits;
using FluidBus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluidBus.Handlers
{
	public class BusLogHandler : FluidHandler<BusLogEvent>
	{
		public	BusLogHandler() : base (IdGenerator.GetNewId(IdGenerator.IdType.Handler))
		{ }

		public override void	Handle(BusLogEvent evt)
		{
			Console.WriteLine(evt.Message);
		}
	}
}
