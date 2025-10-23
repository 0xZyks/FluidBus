using FluidBus.Herits;
using FluidBus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluidBus.Events
{
	public class BusLogEvent : FluidEvent
	{
		public string?	Message { get; private set; }

		public	BusLogEvent(string message) : base (IdGenerator.GetNewId(IdGenerator.IdType.Event))
			=> this.Message = message;
	}
}
