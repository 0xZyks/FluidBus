using FluidBus.FluidBus.Loader.Core;
using FluidBus.FluidBus.Loader.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace FluidBus.FluidBus.Loader.Loader
{
	internal class PELoader
	{
		const uint MEM_COMMIT = 0x1000;
		const uint MEM_RESERVE = 0x2000;
		const uint PAGE_EXECUTE_READWRITE = 0x40;

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr VirtualAlloc(
			IntPtr lpAddress,
			uint dwSize,
			uint flAllocationType,
			uint flProtect
		);
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
		static extern IntPtr LoadLibraryA(string lpLibFileName);

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
		static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

		public static void LoadModule(PEModule module)
		{
			allocImage(module);
			copyHeaders(module);
			copySections(module);
			relocEntries(module);
			loadImport(module);
		}

		private static void allocImage(PEModule module)
			=> module.BaseAddress = VirtualAlloc(
				IntPtr.Zero,
				module.OPTIONAL_HEADER!.SizeOfImage,
				MEM_COMMIT | MEM_RESERVE,
				PAGE_EXECUTE_READWRITE
			);

		private static void copyHeaders(PEModule module)
			=> Marshal.Copy(
				module.Raw,
				0,
				module.BaseAddress,
				(int)module.OPTIONAL_HEADER!.SizeOfHeaders
			);

		private static void copySections(PEModule module)
		{
			foreach (var section in module.SECTION_HEADERS!)
				Marshal.Copy(
					module.Raw,
					(int)section.PointerToRawData,
					module.BaseAddress + (int)section.VirtualAddress,
					(int)section.SizeOfRawData
				);
		}

		private unsafe static void relocEntries(PEModule module)
		{
			long delta = (long)module.BaseAddress - (long)module.OPTIONAL_HEADER!.ImageBase;
			foreach (var (reloc, entries) in module.RELOCS)
				foreach (var entry in entries)
					if (!entry.IsPadding)
					{
						long* ptr = (long*)(module.BaseAddress + reloc.VirtualAddress + entry.Offset);
						*ptr += delta;
					}
		}

		private unsafe static void loadImport(PEModule module)
		{
			foreach (var (import, entries) in module.IMPORTS)
			{
				var name = Helpers.ReadString(Helpers.Resolve(import.Name, module), module);
				IntPtr handle = LoadLibraryA(name);

				for (int i = 0; i < entries.Count; i++)
				{
					var func = entries[i];
					IntPtr funcAddr = GetProcAddress(handle, func.Name!);

					long* ptr = (long*)(module.BaseAddress + import.FirstThunk + i * 8);
					*ptr = funcAddr;
					module.ResolvedImports[func.Name!] = funcAddr;
				}
			}
		}

		private unsafe static void loadExport(PEModule module)
		{
			foreach (var export in module.EXPORT_FUNCS)
			{
				IntPtr realAddr = module.BaseAddress + (int)export.FunctionRVA;
				export.RealAddress = realAddr;
				module.ResolvedExports[export.Name!] = realAddr;
			}
		}
	}
}
