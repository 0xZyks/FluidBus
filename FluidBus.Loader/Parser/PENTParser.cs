using FluidBus.FluidBus.Loader.Core;
using FluidBus.FluidBus.Loader.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FluidBus.FluidBus.Loader.Parser
{
	public class PENTParser
	{
		private PEModule _module;
		public PENTParser(PEModule module)
			=> this._module = module;

		public bool StartParse()
		{
			this.parseDosHeader();
			if (!this.getSig())
				return false;
			this.parseImageFileHeader();
			this.parseOptionalHeader();
			this.parseSections();
			this.parseImport();
			this.parseRelocTable();
			this.parseExportTable();
			return true;
		}

		private void parseDosHeader()
			=> this._module.DOS_HEADER = Helpers.Read<IMAGE_DOS_HEADER>(this._module.Raw, 0);

		private bool getSig()
		{
			if (this._module.DOS_HEADER.e_magic != PE.MZ_SIGNATURE)
				return false;
			this._module.Sig = Helpers.Read<DWORD>(this._module.Raw, this._module.DOS_HEADER.e_lfanew);
			if (this._module.Sig != PE.PE_SIGNATURE)
			{ Console.WriteLine("Not a PE File"); Console.ReadLine(); return false; }
			return true;
		}

		private void parseImageFileHeader()
		{
			this._module.FILE_HEADER = Helpers.Read<IMAGE_FILE_HEADER>(this._module.Raw, this._module.DOS_HEADER.e_lfanew + 4);
			this._module.SECTION_HEADERS = new IMAGE_SECTION_HEADER[this._module.FILE_HEADER.NumberOfSections];
		}

		private void parseOptionalHeader()
		{
			var magic = Helpers.Read<WORD>(this._module.Raw, this._module.DOS_HEADER.e_lfanew + 4 + 20);

			if (magic == PE.OPT_HDR_32)
			{
				this._module.OPTIONAL_HEADER = Helpers.Read<IMAGE_OPTIONAL_HEADER32>(this._module.Raw, this._module.DOS_HEADER.e_lfanew + 4 + 20);
				this._module.IsPE64 = false;
			}
			else if (magic == PE.OPT_HDR_64)
			{
				this._module.OPTIONAL_HEADER = Helpers.Read<IMAGE_OPTIONAL_HEADER64>(this._module.Raw, this._module.DOS_HEADER.e_lfanew + 4 + 20);
				this._module.IsPE64 = true;
			}
		}

		private void parseSections()
		{
			for (int i = 0; i < this._module.SECTION_HEADERS!.Length; i++)
			{
				var sectionOffset = this._module.DOS_HEADER.e_lfanew + 4 + 20 + this._module.FILE_HEADER.SizeOfOptionalHeader + (i * 40);
				this._module.SECTION_HEADERS[i] = Helpers.Read<IMAGE_SECTION_HEADER>(this._module.Raw, sectionOffset);
			}
		}

		private void parseImport()
		{
			var import = this._module.OPTIONAL_HEADER!.GetDirectory(PE.DIR_IMPORT);
			uint fileOffset = Helpers.Resolve(import.VirtualAddress, this._module);
			int index = 0;

			while (true)
			{
				int offset = (int)(fileOffset + index * Marshal.SizeOf<IMAGE_IMPORT_DESCRIPTOR>());
				var descriptor = Helpers.Read<IMAGE_IMPORT_DESCRIPTOR>(this._module.Raw, offset);
				if (descriptor.IsNull)
					break;
				this._module.IMPORTS.Add((descriptor, this.parseILT(descriptor)));
				index++;
			}
		}
		private List<ImportedFunction> parseILT(IMAGE_IMPORT_DESCRIPTOR desc)
		{
			var offset = Helpers.Resolve(desc.OriginalFirstThunk, this._module);
			if (!this._module.IsPE64)
				return loopILT<ILT_ENTRY_32>(offset, sizeof(DWORD));
			else
				return loopILT<ILT_ENTRY_64>(offset, sizeof(QWORD));
		}

		private List<ImportedFunction> loopILT<T>(uint offset, int increment) where T : struct, ILT_ENTRY
		{
			int index = 0;
			List<ImportedFunction> funcs = new();
			while (true)
			{
				var targetOffset = (int)(offset + index * increment);
				var entry = Helpers.Read<T>(this._module.Raw, targetOffset);
				if (entry.IsNull)
					break;
				if (entry.ImportByOrdinal)
					funcs.Add(new ImportedFunction()
					{
						OriginalThunk = (int)entry.HintNameRVA,
						ByOrdinal = entry.ImportByOrdinal,
						Ordinal = entry.Ordinal,
					});
				else
				{
					var hint = Helpers.Resolve(entry.HintNameRVA, this._module);
					var import = Helpers.Read<IMAGE_IMPORT_BY_NAME>(this._module.Raw, (int)hint);
					funcs.Add(new ImportedFunction()
					{
						OriginalThunk = (int)entry.HintNameRVA,
						ByOrdinal = entry.ImportByOrdinal,
						Hint = import.Hint,
						Name = import.Name()
					});
				}
				index++;
			}
			return funcs;
		}

		private void parseRelocTable()
		{
			var reloc = this._module.OPTIONAL_HEADER!.GetDirectory(PE.DIR_BASERELOC);
			uint fileOffset = Helpers.Resolve(reloc.VirtualAddress, this._module);
			uint index = 0;

			while (true)
			{
				int offset = (int)(fileOffset + index);
				var base_reloc = Helpers.Read<IMAGE_BASE_RELOCATION>(this._module.Raw, offset);
				if (base_reloc.IsNull)
					break;
				var blocks = new List<BASE_RELOC_ENTRY>();
				for (int i = 0; i < base_reloc.EntryCount; i++)
				{
					int entryOffset = offset + 8 + (i * sizeof(WORD));
					blocks.Add(Helpers.Read<BASE_RELOC_ENTRY>(this._module.Raw, entryOffset));
				}
				this._module.RELOCS.Add((base_reloc, blocks));
				index += base_reloc.SizeOfBlock;
			}
		}

		private void parseExportTable()
		{
			var export = this._module.OPTIONAL_HEADER!.GetDirectory(PE.DIR_EXPORT);
			if (export.IsEmpty)
				return;
			uint fileOffset = Helpers.Resolve(export.VirtualAddress, this._module);
			var exportDir = Helpers.Read<IMAGE_EXPORT_DIRECTORY>(this._module.Raw, (int)fileOffset);
			this._module.EXPORT_DIR = exportDir;

			uint namesTableOffset = Helpers.Resolve(exportDir.AddressOfNames, this._module);
			uint ordinalTableOffset = Helpers.Resolve(exportDir.AddressOfNameOrdinals, this._module);
			uint funcTableOffset = Helpers.Resolve(exportDir.AddressOfFunctions, this._module);

			for (int i = 0; i < exportDir.NumberOfNames; i++)
			{
				uint nameRva = Helpers.Read<DWORD>(this._module.Raw, (int)(namesTableOffset + i * 4));
				uint nameOffset = Helpers.Resolve(nameRva, this._module);

				WORD ordinalRva = Helpers.Read<WORD>(this._module.Raw, (int)(ordinalTableOffset + i * 2));

				DWORD funcRva = Helpers.Read<DWORD>(this._module.Raw, (int)(funcTableOffset + ordinalRva * 4));

				this._module.EXPORT_FUNCS.Add(new ExportedFunction()
				{
					Name = Helpers.ReadString(nameOffset, this._module),
					Ordinal = ordinalRva,
					FunctionRVA = funcRva,
				});
			}
		}
	}
}
