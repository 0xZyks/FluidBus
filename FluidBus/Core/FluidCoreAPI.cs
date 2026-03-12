using System.Runtime.InteropServices;
using System.Text;

namespace FluidBus.Core
{
    public static class FluidCoreAPI
    {
        [DllImport("libfluid_core", EntryPoint = "process_bytes")]
        private static extern IntPtr ProcessBytes(byte[] data, nuint len, out nuint outLen);

        [DllImport("libfluid_core", EntryPoint = "free_bytes")]
        private static extern void FreeBytes(IntPtr ptr, nuint len);

        [DllImport("libfluid_core", EntryPoint = "init")]
        private static extern ulong Init();

        [DllImport("libfluid_core", EntryPoint = "get_token")]
        private static extern IntPtr GetToken(byte opcode, out nuint outLen);

        [DllImport("libfluid_core", EntryPoint = "rotate_token")]
        private static extern IntPtr RotateToken(byte[] data, nuint len, out nuint outLen);

        [DllImport("libfluid_core", EntryPoint = "get_bytecode")]
        private static extern IntPtr GetBytecode(
            byte opcode,
            byte[] typeName, nuint typeNameLen,
            byte[] methodName, nuint methodNameLen,
            byte[] argType, nuint argTypeLen,
            byte[] arg, nuint argLen,
            out nuint outLen);

        [DllImport("libfluid_core", EntryPoint = "get_parsed_bytecode_by_token")]
        private static extern IntPtr GetParsedBytecodeByToken(
                byte[] token, nuint tokenLen,
                byte[] arg, nuint argLen,
                out nuint outLen);

        // --- Methodes publiques existantes ---

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
            => Array.Empty<byte>();

        public static object? Execute(byte[] bytecode)
            => null;

        public static byte[] GetBytecode(byte opcode, string typeName, string methodName, string argType, byte[] arg)
        {
            byte[] typeBytes = Encoding.UTF8.GetBytes(typeName);
            byte[] methodBytes = Encoding.UTF8.GetBytes(methodName);
            byte[] argTypeBytes = Encoding.UTF8.GetBytes(argType);

            IntPtr ptr = GetBytecode(
                opcode,
                typeBytes, (nuint)typeBytes.Length,
                methodBytes, (nuint)methodBytes.Length,
                argTypeBytes, (nuint)argTypeBytes.Length,
                arg, (nuint)arg.Length,
                out nuint outLen);

            byte[] result = new byte[outLen];
            Marshal.Copy(ptr, result, 0, (int)outLen);
            FreeBytes(ptr, outLen);
            return result;
        }

        // --- Nouvelle methode ---

        public static (ParsedMethod? Method, byte[] NextToken) ParseBytecodeByToken(byte[] token, byte[]? arg = null)
        {
            arg ??= Array.Empty<byte>();
            IntPtr ptr = GetParsedBytecodeByToken(
                    token, (nuint)token.Length,
                    arg, (nuint)arg.Length,
                    out nuint outLen);

            if (ptr == IntPtr.Zero)
                return (null, Array.Empty<byte>());

            byte[] data = new byte[outLen];
            Marshal.Copy(ptr, data, 0, (int)outLen);
            Free(ptr, outLen);

            int i = 0;

            int nextTokenLen = data[i++];
            byte[] nextToken = data[i..(i + nextTokenLen)];
            i += nextTokenLen;

            int typeLen = data[i++];
            string typeName = Encoding.UTF8.GetString(data, i, typeLen);
            i += typeLen;

            int methodLen = data[i++];
            string methodName = Encoding.UTF8.GetString(data, i, methodLen);
            i += methodLen;

            int argTypeLen = data[i++];
            string argType = Encoding.UTF8.GetString(data, i, argTypeLen);
            i += argTypeLen;

            int argLen2 = data[i++];
            byte[] argData = data[i..(i + argLen2)];

            return (new ParsedMethod(typeName, methodName, argType, argData), nextToken);
        }
    }

    public record ParsedMethod(
        string TypeName,
        string MethodName,
        string ArgType,
        byte[] Arg
    );
}
