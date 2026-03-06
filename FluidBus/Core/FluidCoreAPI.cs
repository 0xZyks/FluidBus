using FluidBus.Core.Herits;
using FluidBus.Core.Instructions;
using System.Runtime.InteropServices;

namespace FluidBus.Core
{
    public static class FluidCoreAPI
    {
        [DllImport("libfluid_core", EntryPoint="process_bytes")]
        private static extern IntPtr ProcessBytes(byte[] data, nuint len, out nuint outLen);

        [DllImport("libfluid_core", EntryPoint = "free_bytes")]
        private static extern void FreeBytes(IntPtr ptr, nuint len);

        public static byte[] Send(byte[][] data)
        {
            byte[] flat = data.SelectMany(x => x).ToArray();
            IntPtr resultPtr = ProcessBytes(flat, (nuint)flat.Length, out nuint outLen);
            byte[] result = new byte[outLen];
            Marshal.Copy(resultPtr, result, 0, (int)outLen);
            FreeBytes(resultPtr, outLen);
            return result;
        }
    }
}
