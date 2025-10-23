using FluidBus.Herits;
using FluidBus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluidBus.Mocks
{
	public class JunkHandler : FluidHandler<JunkEvent>
	{
		public JunkHandler() : base(IdGenerator.GetNewId(IdGenerator.IdType.Handler)) { }
		public override void	Handle(JunkEvent evt)
		{
			Console.WriteLine("Coucou");
		}
	}
}
