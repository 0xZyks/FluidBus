using System.Runtime.InteropServices;
using System.Text;

namespace FluidBus.Core
{
    public static unsafe class FluidCoreAPI
    {
        // --- Function pointers ---
        private static readonly delegate* unmanaged<nuint, ulong> _init;
        private static readonly delegate* unmanaged<byte*, nuint, nuint*, IntPtr> _processBytes;
        private static readonly delegate* unmanaged<IntPtr, nuint, void> _freeBytes;
        private static readonly delegate* unmanaged<byte, nuint*, IntPtr> _getToken;
        private static readonly delegate* unmanaged<byte*, nuint, nuint*, IntPtr> _rotateToken;
        private static readonly delegate* unmanaged<byte, byte*, nuint, byte*, nuint, byte*, nuint, byte*, nuint, byte*, nuint, nuint*, IntPtr> _getBytecode;
        private static readonly delegate* unmanaged<byte*, nuint, byte*, nuint, nuint*, IntPtr> _getParsedBytecodeByToken;

        static FluidCoreAPI()
        {
            NativeLibrary.SetDllImportResolver(typeof(FluidCoreAPI).Assembly, (name, assembly, path) =>
                    {
                    if (name == "libfluid_core")
                    return NativeLibrary.Load("libfluid_core", assembly, path);
                    return IntPtr.Zero;
                    });

            var lib = NativeLibrary.Load("libfluid_core", typeof(FluidCoreAPI).Assembly, null);

            _init                     = (delegate* unmanaged<nuint, ulong>) NativeLibrary.GetExport(lib, "init");
            _processBytes             = (delegate* unmanaged<byte*, nuint, nuint*, IntPtr>) NativeLibrary.GetExport(lib, "process_bytes");
            _freeBytes                = (delegate* unmanaged<IntPtr, nuint, void>) NativeLibrary.GetExport(lib, "free_bytes");
            _getToken                 = (delegate* unmanaged<byte, nuint*, IntPtr>) NativeLibrary.GetExport(lib, "get_token");
            _rotateToken              = (delegate* unmanaged<byte*, nuint, nuint*, IntPtr>) NativeLibrary.GetExport(lib, "rotate_token");
            _getBytecode              = (delegate* unmanaged<byte, byte*, nuint, byte*, nuint, byte*, nuint, byte*, nuint, byte*, nuint, nuint*, IntPtr>) NativeLibrary.GetExport(lib, "get_bytecode");
            _getParsedBytecodeByToken = (delegate* unmanaged<byte*, nuint, byte*, nuint, nuint*, IntPtr>) NativeLibrary.GetExport(lib, "get_parsed_bytecode_by_token");

            _init(0);
        }

        // --- Helpers internes ---

        private static byte[] CopyAndFree(IntPtr ptr, nuint outLen)
        {
            byte[] result = new byte[outLen];
            Marshal.Copy(ptr, result, 0, (int)outLen);
            _freeBytes(ptr, outLen);
            return result;
        }

        private static byte[] SerializeArgs(byte[][] args)
        {
            var buf = new List<byte> { (byte)args.Length };
            foreach (var arg in args)
            {
                buf.Add((byte)arg.Length);
                buf.AddRange(arg);
            }
            return buf.ToArray();
        }

        // --- API publique ---

        public static ulong Initialize()
        {
            // Deja appele dans le static ctor, expose pour compatibilite
            return _init(0);
        }

        public static byte[] Send(byte[] data)
        {
            fixed (byte* ptr = data)
            {
                nuint outLen;
                IntPtr result = _processBytes(ptr, (nuint)data.Length, &outLen);
                return CopyAndFree(result, outLen);
            }
        }

        public static void Free(IntPtr ptr, nuint outLen)
            => _freeBytes(ptr, outLen);

        public static byte[] RequestToken(byte opcode)
        {
            nuint outLen;
            IntPtr ptr = _getToken(opcode, &outLen);
            return CopyAndFree(ptr, outLen);
        }

        public static byte[] Rotate(byte[] token)
        {
            fixed (byte* ptr = token)
            {
                nuint outLen;
                IntPtr result = _rotateToken(ptr, (nuint)token.Length, &outLen);
                return CopyAndFree(result, outLen);
            }
        }

        public static byte[] GetBytecode(byte opcode, string typeName, string methodName, string argType, byte[] arg)
        {
            byte[] typeBytes   = Encoding.UTF8.GetBytes(typeName);
            byte[] methodBytes = Encoding.UTF8.GetBytes(methodName);
            byte[] argTypeBytes = Encoding.UTF8.GetBytes(argType);

            fixed (byte* tPtr = typeBytes)
            fixed (byte* mPtr = methodBytes)
            fixed (byte* atPtr = argTypeBytes)
            fixed (byte* aPtr = arg)
            {
                nuint outLen;
                IntPtr ptr = _getBytecode(
                    opcode,
                    tPtr,  (nuint)typeBytes.Length,
                    mPtr,  (nuint)methodBytes.Length,
                    atPtr, (nuint)argTypeBytes.Length,
                    aPtr,  (nuint)arg.Length,
                    null, 0,
                    &outLen);
                return CopyAndFree(ptr, outLen);
            }
        }

        public static (ParsedMethod? Method, byte[] NextToken) ParseBytecodeByToken(byte[] token, byte[][]? args = null)
        {
            byte[] argsBuf = args is null ? [0] : SerializeArgs(args);

            fixed (byte* tPtr = token)
            fixed (byte* aPtr = argsBuf)
            {
                nuint outLen;
                IntPtr ptr = _getParsedBytecodeByToken(
                    tPtr, (nuint)token.Length,
                    aPtr, (nuint)argsBuf.Length,
                    &outLen);

                if (ptr == IntPtr.Zero)
                    return (null, Array.Empty<byte>());

                byte[] data = CopyAndFree(ptr, outLen);
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

                int nbArgs = data[i++];
                var parsedArgs = new byte[nbArgs][];
                for (int j = 0; j < nbArgs; j++)
                {
                    int argLen = data[i++];
                    parsedArgs[j] = data[i..(i + argLen)];
                    i += argLen;
                }

                return (new ParsedMethod(typeName, methodName, argType, parsedArgs), nextToken);
            }
        }

        public static byte[] GetMethod(byte[] token) => Array.Empty<byte>();
        public static object? Execute(byte[] bytecode) => null;
    }

    public record ParsedMethod(
        string TypeName,
        string MethodName,
        string ArgType,
        byte[][] Args
    );
}
