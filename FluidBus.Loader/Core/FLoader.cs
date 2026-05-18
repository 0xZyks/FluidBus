using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.FluidBus.Loader.Core
{
	public class FLoader
	{
		public static bool LoadModule(string filePath)
		{
			PEModule mod = new PEModule(filePath);
			if (mod.ParseModule())
				return true;
			return false;
		}

		public static bool UnloadModule(string name)
		{
			return true;
		}
	}
}
