using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluidBus.Utils
{
	public class IdGenerator
	{
		public enum IdType
		{
			Handler = 0,
			Event = 1,
		}

		public static string GetNewId(IdType type)
		{
			string	res = string.Empty;
			string	_base = "$Fluid";

			res += _base;

			switch (type)
			{
				case IdType.Handler:
					res += "-HDL-";
				break;
				case IdType.Event:
					res += "-EVT-";
				break;
				default:
					res += "NULL";
				return res;
			}
			res += Guid.NewGuid().ToString().Substring(0, 8);

			return (res);
		}
	}
}
