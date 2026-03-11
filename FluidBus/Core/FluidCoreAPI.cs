using FluidBus.Core.Herits;
using FluidBus.Core.Instructions;
using System.Runtime.InteropServices;

namespace FluidBus.Core
{
    public static class FluidCoreAPI
    {
        [DllImport("libfluid_core", EntryPoint="process_bytes")]
        private static extern IntPtr ProcessBytes(byte[] data, nuint len, out nuint outLen);

        [DllImport("libfluid_core", EntryPoint="free_bytes")]
        private static extern void FreeBytes(IntPtr ptr, nuint len);

        [DllImport("libfluid_core", EntryPoint="init")]
        private static extern ulong Init();

        [DllImport("libfluid_core", EntryPoint="get_token")]
        private static extern IntPtr GetToken(byte opcode, out nuint outLen);

        [DllImport("libfluid_core", EntryPoint="rotate_token")]
        private static extern IntPtr RotateToken(byte[] data, nuint len, out nuint outLen);

        public static byte[] Send(byte[] data)
        {
            IntPtr resultPtr = ProcessBytes(data, (nuint)data.Length, out nuint outLen);
            byte[] result = new byte[outLen];
            Marshal.Copy(resultPtr, result, 0, (int)outLen);
            Free(resultPtr, outLen);
            return result;
        }

        public static void Free(IntPtr ptr, nuint outLen)
            => FreeBytes(ptr, outLen);

        public static ulong Initialize()
            => Init();

        public static byte[] RequestToken(byte opcode)
        {
            IntPtr ptr = GetToken(opcode, out nuint outLen);
            byte[] token = new byte[outLen];
            Marshal.Copy(ptr, token, 0, (int)outLen);
            Free(ptr, outLen);
            return token;
        }

        public static byte[] Rotate(byte[] token)
        {
            IntPtr ptr = RotateToken(token, (nuint)token.Length, out nuint outLen);
            byte[] next = new byte[outLen];
            Marshal.Copy(ptr, next, 0, (int)outLen);
            Free(ptr, outLen);
            return next;
        }

        public static byte[] GetMethod(byte[] token)
        {
            return new byte[0];
        }

        public static object? Execute(byte[] bytecode)
        {
            return null!;
        }
    }
}
