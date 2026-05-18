using FluidBus.FluidBus.Loader.Core;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FluidBus.FluidBus.Loader.Utils
{
	public class Helpers
	{
		public static T Read<T>(byte[] raw, int offset) where T : struct
			=> MemoryMarshal.Read<T>(raw.AsSpan(offset));
		public static uint Resolve(uint rva, PEModule module)
		{
			foreach (var section in module.SECTION_HEADERS!)
				if (section.ContainsRVA(rva))
					return section.ResolveRVA(rva);
			throw new Exception($"RVA: 0x{rva:X} not found in any section");
		}

		public static string ReadString(uint offset, PEModule module)
		{
			var bytes = new List<byte>();
			while (module.Raw[offset] != 0)
				bytes.Add(module.Raw[offset++]);
			return Encoding.UTF8.GetString(bytes.ToArray());
		}
	}
}
