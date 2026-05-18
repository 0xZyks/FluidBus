global using BYTE = System.Byte;
global using WORD = System.UInt16;
global using DWORD = System.UInt32;
global using QWORD = System.UInt64;
global using LONG = System.Int32;
global using ULONGLONG = System.UInt64;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FluidBus.FluidBus.Loader.Utils
{
	public static class PE
	{
		public const WORD MZ_SIGNATURE = 0x5A4D;
		public const DWORD PE_SIGNATURE = 0x00004550;
		public const WORD OPT_HDR_32 = 0x10B;
		public const WORD OPT_HDR_64 = 0x20B;
		public const int DIR_EXPORT = 0;
		public const int DIR_IMPORT = 1;
		public const int DIR_BASERELOC = 5;
		public const int DIR_TLS = 9;
		public const int DIR_IAT = 12;
	}

	public class ImportedFunction
	{
		public bool ByOrdinal { get; set; }
		public WORD Ordinal { get; set; }
		public WORD Hint { get; set; }
		public string? Name { get; set; }
		public int? OriginalThunk { get; set; }
	}

	public class ExportedFunction
	{
		public string? Name { get; set; }
		public WORD Ordinal { get; set; }
		public DWORD FunctionRVA { get; set; }
		public IntPtr RealAddress { get; set; }
	}

	[StructLayout(LayoutKind.Explicit, Pack = 1)]
	public struct IMAGE_DOS_HEADER
	{
		[FieldOffset(0x00)] public WORD e_magic;
		[FieldOffset(0x3C)] public LONG e_lfanew;
	}


	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct IMAGE_FILE_HEADER
	{
		public WORD Machine;
		public WORD NumberOfSections;
		private DWORD _timestamp;
		private DWORD _symTablePtr;
		private DWORD _symCount;
		public WORD SizeOfOptionalHeader;
		private WORD _characteristics;
	}


	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct IMAGE_DATA_DIRECTORY
	{
		public DWORD VirtualAddress;
		public DWORD Size;

		public bool IsEmpty => VirtualAddress == 0 || Size == 0;
	}

	public interface OPTIONAL_HEADER
	{
		WORD Magic { get; }
		DWORD AddressOfEntryPoint { get; }
		ULONGLONG ImageBase { get; }
		DWORD SectionAlignment { get; }
		DWORD FileAlignment { get; }
		DWORD SizeOfHeaders { get; }
		DWORD SizeOfImage { get; }
		IMAGE_DATA_DIRECTORY GetDirectory(int index);
	}


	[StructLayout(LayoutKind.Explicit, Pack = 1)]
	public unsafe struct IMAGE_OPTIONAL_HEADER32 : OPTIONAL_HEADER
	{
		[FieldOffset(0x00)] private WORD _magic;
		[FieldOffset(0x10)] private DWORD _addressOfEntryPoint;
		[FieldOffset(0x1C)] private DWORD _imageBase;
		[FieldOffset(0x20)] private DWORD _sectionAlignment;
		[FieldOffset(0x24)] private DWORD _fileAlignment;
		[FieldOffset(0x38)] private DWORD _sizeOfImage;
		[FieldOffset(0x3C)] private DWORD _sizeOfHeaders;
		[FieldOffset(0x60)] private fixed byte _dataDirectory[16 * 8];


		public WORD Magic => _magic;
		public DWORD AddressOfEntryPoint => _addressOfEntryPoint;
		public ULONGLONG ImageBase => _imageBase;
		public DWORD SectionAlignment => _sectionAlignment;
		public DWORD FileAlignment => _fileAlignment;
		public DWORD SizeOfImage => _sizeOfImage;
		public DWORD SizeOfHeaders => _sizeOfHeaders;
		public unsafe IMAGE_DATA_DIRECTORY GetDirectory(int index)
		{
			fixed (byte* p = _dataDirectory)
				return ((IMAGE_DATA_DIRECTORY*)p)[index];
		}
	}


	[StructLayout(LayoutKind.Explicit, Pack = 1)]
	public unsafe struct IMAGE_OPTIONAL_HEADER64 : OPTIONAL_HEADER
	{
		[FieldOffset(0x00)] private WORD _magic;
		[FieldOffset(0x10)] private DWORD _addressOfEntryPoint;
		[FieldOffset(0x18)] private ULONGLONG _imageBase;
		[FieldOffset(0x20)] private DWORD _sectionAlignment;
		[FieldOffset(0x24)] private DWORD _fileAlignment;
		[FieldOffset(0x38)] private DWORD _sizeOfImage;
		[FieldOffset(0x3C)] private DWORD _sizeOfHeaders;
		[FieldOffset(0x70)] private fixed byte _dataDirectory[16 * 8];

		public WORD Magic => _magic;
		public DWORD AddressOfEntryPoint => _addressOfEntryPoint;
		public ULONGLONG ImageBase => _imageBase;
		public DWORD SectionAlignment => _sectionAlignment;
		public DWORD FileAlignment => _fileAlignment;
		public DWORD SizeOfImage => _sizeOfImage;
		public DWORD SizeOfHeaders => _sizeOfHeaders;

		public unsafe IMAGE_DATA_DIRECTORY GetDirectory(int index)
		{
			fixed (byte* p = _dataDirectory)
				return ((IMAGE_DATA_DIRECTORY*)p)[index];
		}
	}


	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct IMAGE_SECTION_HEADER
	{
		private fixed BYTE _name[8];
		public DWORD VirtualSize;
		public DWORD VirtualAddress;
		public DWORD SizeOfRawData;
		public DWORD PointerToRawData;
		private DWORD _relocPtr, _linePtr;
		private WORD _relocCount, _lineCount;
		public DWORD Characteristics;

		public string Name()
		{
			fixed (byte* p = _name)
			{
				byte[] name = new byte[8];
				Marshal.Copy((IntPtr)p, name, 0, name.Length);
				return Encoding.UTF8.GetString(name).TrimEnd('\0');
			}
		}

		public bool ContainsRVA(DWORD rva) =>
			rva >= VirtualAddress && rva < VirtualAddress + VirtualSize;

		public DWORD ResolveRVA(DWORD rva) =>
			(rva - VirtualAddress) + PointerToRawData;
	}


	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct IMAGE_IMPORT_DESCRIPTOR
	{
		public DWORD OriginalFirstThunk;
		public DWORD TimeDateStamp;
		public DWORD ForwarderChain;
		public DWORD Name;
		public DWORD FirstThunk;
		public bool IsNull => Name == 0 && FirstThunk == 0;
	}

	public interface ILT_ENTRY
	{
		bool IsNull { get; }
		bool ImportByOrdinal { get; }
		WORD Ordinal { get; }
		DWORD HintNameRVA { get; }
	}

	public readonly struct ILT_ENTRY_32 : ILT_ENTRY
	{
		private readonly DWORD _v;
		public bool IsNull => _v == 0;
		public bool ImportByOrdinal => ((_v >> 31) & 1) == 1;
		public WORD Ordinal => (WORD)(_v & 0xFFFF);
		public DWORD HintNameRVA => _v & 0x7FFFFFFF;
	}

	public readonly struct ILT_ENTRY_64 : ILT_ENTRY
	{
		private readonly QWORD _v;
		public bool IsNull => _v == 0;
		public bool ImportByOrdinal => ((_v >> 63) & 1) == 1;
		public WORD Ordinal => (WORD)(_v & 0xFFFF);
		public DWORD HintNameRVA => (DWORD)(_v & 0x7FFFFFFF);
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct IMAGE_EXPORT_DIRECTORY
	{
		private DWORD _characteristics;
		private DWORD _timeDateStamp;
		private WORD _majorVersion;
		private WORD _minorVersion;
		public DWORD Name;
		public DWORD Base;
		public DWORD NumberOfFunctions;
		public DWORD NumberOfNames;
		public DWORD AddressOfFunctions;
		public DWORD AddressOfNames;
		public DWORD AddressOfNameOrdinals;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct IMAGE_EXPORT_BY_NAME
	{
		public WORD Hint;
		private fixed byte _name[256];

		public string Name()
		{
			fixed (byte* p = _name)
			{
				int len = 0;
				while (len < 256 && p[len] != 0)
					len++;
				return Encoding.UTF8.GetString(p, len);
			}
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public unsafe struct IMAGE_IMPORT_BY_NAME
	{
		public WORD Hint;
		private fixed byte _name[256];

		public string Name()
		{
			fixed (byte* p = _name)
			{
				int len = 0;
				while (len < 256 && p[len] != 0)
					len++;
				return Encoding.UTF8.GetString(p, len);
			}
		}
	}


	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct IMAGE_BASE_RELOCATION
	{
		public DWORD VirtualAddress;
		public DWORD SizeOfBlock;

		public int EntryCount => ((int)SizeOfBlock - 8) / 2;
		public bool IsNull => VirtualAddress == 0 && SizeOfBlock == 0;
	}

	public readonly struct BASE_RELOC_ENTRY
	{
		private readonly WORD _v;
		public BASE_RELOC_ENTRY(WORD raw) => _v = raw;

		public WORD Type => (WORD)((_v >> 12) & 0xF);
		public WORD Offset => (WORD)(_v & 0x0FFF);
		public bool IsPadding => Type == 0;
	}
}
