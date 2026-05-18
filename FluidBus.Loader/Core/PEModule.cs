using FluidBus.FluidBus.Loader.Parser;
using FluidBus.FluidBus.Loader.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluidBus.FluidBus.Loader.Core
{
	public class PEModule
	{
		private byte[] _raw;
		private bool isPE64;

		public IMAGE_DOS_HEADER DOS_HEADER { get; set; }
		public IMAGE_FILE_HEADER FILE_HEADER { get; set; }
		public OPTIONAL_HEADER? OPTIONAL_HEADER { get; set; }
		public IMAGE_SECTION_HEADER[]? SECTION_HEADERS { get; set; }
		public IMAGE_EXPORT_DIRECTORY EXPORT_DIR { get; set; }
		public List<ExportedFunction> EXPORT_FUNCS { get; set; }
		public List<(IMAGE_IMPORT_DESCRIPTOR, List<ImportedFunction>)> IMPORTS { get; set; }
		public List<(IMAGE_BASE_RELOCATION, List<BASE_RELOC_ENTRY>)> RELOCS { get; private set; }
		public Dictionary<string, IntPtr> ResolvedImports { get; set; }
		public Dictionary<string, IntPtr> ResolvedExports { get; set; }
		public DWORD Sig { get; set; }

		public byte[] Raw
			=> this._raw;
		public bool IsPE64
		{ get { return this.isPE64; } set { this.isPE64 = value; } }
		public IntPtr BaseAddress { get; set; }

		public PEModule(string target)
		{
			this._raw = File.ReadAllBytes(target);
			this.RELOCS = new();
			this.EXPORT_FUNCS = new();
			this.IMPORTS = new();
			this.ResolvedImports = new();
			this.ResolvedExports = new();
		}

		public bool ParseModule()
		{
			PENTParser parser = new PENTParser(this);
			return parser.StartParse();
		}

		#region DISPLAY
		public void Display()
		{
			Console.WriteLine("== DOS_HEADER ==");
			Console.WriteLine($"Magic: 0x{this.DOS_HEADER.e_magic:X4}");
			Console.WriteLine($"e_lfanew: 0x{this.DOS_HEADER.e_lfanew:X}");

			Console.WriteLine("\n== FILE_HEADER ==");
			Console.WriteLine($"Machine: 0x{this.FILE_HEADER.Machine:X}");
			Console.WriteLine($"Sections: 0x{this.FILE_HEADER.NumberOfSections}");
			Console.WriteLine($"SizeOptHeader: 0x{this.FILE_HEADER.SizeOfOptionalHeader}");
			this.displayOptionalHeader();
			this.displaySections();
			this.displayImportDescriptor();
			this.displayReloc();
			this.displayExport();
		}

		private void displayOptionalHeader()
		{
			if (!this.isPE64)
				Console.WriteLine("\n== PE32_OPTIONAL ==");
			else
				Console.WriteLine("\n == PE64_OPTIONAL ==");
			Console.WriteLine($"EntryPoint: 0x{this.OPTIONAL_HEADER!.AddressOfEntryPoint:X}");
			Console.WriteLine($"ImageSize: 0x{this.OPTIONAL_HEADER.SizeOfImage:X} - {this.OPTIONAL_HEADER.SizeOfImage}");
			Console.WriteLine($"Headers Size: 0x{this.OPTIONAL_HEADER.SizeOfHeaders:X} - {this.OPTIONAL_HEADER.SizeOfHeaders}");
			Console.WriteLine($"ImageBase: 0x{this.OPTIONAL_HEADER.ImageBase:X} - {this.OPTIONAL_HEADER.ImageBase}");
			Console.WriteLine($"File Align: 0x{this.OPTIONAL_HEADER.FileAlignment:X} - {this.OPTIONAL_HEADER.FileAlignment}");
			Console.WriteLine($"Section Align: 0x{this.OPTIONAL_HEADER.SectionAlignment:X} - {this.OPTIONAL_HEADER.SectionAlignment}");
			Console.WriteLine($"Magic: 0x{this.OPTIONAL_HEADER.Magic:X} - {this.OPTIONAL_HEADER.Magic}");
			var reloc = this.OPTIONAL_HEADER.GetDirectory(PE.DIR_BASERELOC);
			var tls = this.OPTIONAL_HEADER.GetDirectory(PE.DIR_TLS);
			var iat = this.OPTIONAL_HEADER.GetDirectory(PE.DIR_IAT);
			var import = this.OPTIONAL_HEADER.GetDirectory(PE.DIR_IMPORT);
			Console.WriteLine($"Import RVA: 0x{import.VirtualAddress:X} - Size: {import.Size}");
			Console.WriteLine($"Reloc RVA: 0x{reloc.VirtualAddress:X} - Size: {reloc.Size}");
			Console.WriteLine($"IAT RVA: 0x{iat.VirtualAddress:X} - Size: {iat.Size}");
			Console.WriteLine($"TLS RVA: 0x{tls.VirtualAddress:X} - Size: {tls.Size}");
		}

		private void displaySections()
		{
			for (int i = 0; i < this.SECTION_HEADERS!.Length; i++)
			{
				var section = this.SECTION_HEADERS[i];
				Console.WriteLine($"\n== SECTIONS n.{i + 1}==");
				Console.WriteLine($"Name: {section.Name()}");
				Console.WriteLine($"PtrToRaw: 0x{section.PointerToRawData:X} - {section.PointerToRawData}");
				Console.WriteLine($"VirtualAddress: 0x{section.VirtualAddress:X}");
				Console.WriteLine($"VirtualSize: 0x{section.VirtualSize:X} - {section.VirtualSize}");
				Console.WriteLine($"SizeOfRaw: 0x{section.SizeOfRawData:X} - {section.SizeOfRawData}");
				Console.WriteLine($"Characteristics: {section.Characteristics:X} - {section.Characteristics}");
			}
		}

		private void displayImportDescriptor()
		{
			int index = 1;
			foreach (var (import, entries) in this.IMPORTS)
			{
				Console.WriteLine($"\n== IMPORT_DESCRIPTOR n.{index++}==");
				Console.WriteLine($"Name: 0x{import.Name:X} - {Helpers.ReadString(Helpers.Resolve(import.Name, this), this)}");
				Console.WriteLine($"ForwardedChain: 0x{import.ForwarderChain:X} - {import.ForwarderChain}");
				Console.WriteLine($"FirstThunk: 0x{import.FirstThunk:X} - {import.FirstThunk}");
				Console.WriteLine($"OriginalFirstThunk: 0x{import.OriginalFirstThunk:X} - {import.OriginalFirstThunk}");
				Console.WriteLine($"TimeDateStamp: 0x{import.TimeDateStamp:X} - {import.TimeDateStamp}");
				int indexEntries = 1;
				foreach (var func in entries)
				{
					Console.WriteLine($"\n  == ILT_FUNC n.{indexEntries++}==");
					Console.WriteLine($"  OriginalThunk: 0x{func.OriginalThunk:X} - {func.OriginalThunk}");
					Console.WriteLine($"  Name: 0x{func.Name:X} - {func.Name}");
					Console.WriteLine($"  ByOrdinal: 0x{func.ByOrdinal:X} - {func.ByOrdinal}");
					Console.WriteLine($"  Ordinal: 0x{func.Ordinal:X} - {func.Ordinal}");
					Console.WriteLine($"  Hint: 0x{func.Hint:X} - {func.Hint}");
				}
			}
		}

		private void displayReloc()
		{
			int index = 1;
			foreach (var (base_reloc, entries) in this.RELOCS)
			{
				int entriesCount = 1;
				Console.WriteLine($"\n== BASE_RELOC n.{index++} ==");
				Console.WriteLine($"VirtualAddress: {base_reloc.VirtualAddress:X} - {base_reloc.VirtualAddress}");
				Console.WriteLine($"SizeOfBlock: {base_reloc.SizeOfBlock:X} - {base_reloc.SizeOfBlock}");
				Console.WriteLine($"EntryCount: {base_reloc.EntryCount:X} - {base_reloc.EntryCount}\n");
				foreach (var entry in entries)
				{
					Console.WriteLine($"    == Entry n.{entriesCount++} ==");
					Console.WriteLine($"Offset: {entry.Offset:X} - {entry.Offset}");
					Console.WriteLine($"IsPadding: {entry.IsPadding:X} - {entry.IsPadding}");
					Console.WriteLine($"Type: {entry.Type:X} - {entry.Type}");
				}
			}
		}

		private void displayExport()
		{
			int index = 1;
			foreach (var func in this.EXPORT_FUNCS)
			{
				Console.WriteLine($"\n== Export n.{index++}");
				Console.WriteLine($"Name: {func.Name} - {BitConverter.ToInt32(Encoding.UTF8.GetBytes(func.Name!)):X}");
				Console.WriteLine($"Ordinal: 0x{func.Ordinal:X} - {func.Ordinal}");
				Console.WriteLine($"FuncRVA: 0x{func.FunctionRVA:X} - {func.FunctionRVA}");
			}
		}
		#endregion
	}
}
