using FluidBus.Core.Abstracts;
using FluidBus.FluidBus.Memory.Core;
using FluidBus.Memory.Abstracts;

namespace FluidBus.Memory.Core
{
	public static class FMemory
	{
		private static Dictionary<string, FluidProcess> _processes = new();

		public static FluidIPC Connect(string pName, FluidCallBack callback)
		{
			if (_processes.TryGetValue(pName, out var process))
			{
				process.AddConnection(callback);
				return new FluidIPC(pName);
			}
			_processes[pName] = new FluidProcess(pName);

			return new FluidIPC(pName);
		}

		public static void Test()
		{
			Connect("Coucou", Coucou);
		}

		public static object? Coucou(object test)
		{
			return null;
		}
	}
}
