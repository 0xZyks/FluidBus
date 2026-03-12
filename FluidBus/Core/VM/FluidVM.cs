using System.Reflection;
using System.Text;

namespace FluidBus.Core.VM
{
    public record VMResult(
        string Method,
        byte[] NextToken,
        object? Result
    );

    public static class FluidVM
    {
        private static readonly Dictionary<string, string> TypeMapping = new()
        {
            { "Console", "System.Console" },
            { "Math",    "System.Math"    },
            { "String",  "System.String"  },
            { "Convert", "System.Convert" },
        };

        public static VMResult? Run(byte[] token, byte[]? arg = null)
        {
            var (parsed, nextToken) = FluidCoreAPI.ParseBytecodeByToken(token, arg);
            if (parsed is null)
                return null;

            string fullTypeName = parsed.TypeName switch
            {
                "Console" => "System.Console",
                "Math"    => "System.Math",
                "String"  => "System.String",
                "Convert" => "System.Convert",
                _         => parsed.TypeName
            };

            Type? type = Type.GetType(fullTypeName)
                      ?? AppDomain.CurrentDomain.GetAssemblies()
                             .Select(a => a.GetType(fullTypeName))
                             .FirstOrDefault(t => t != null);

            if (type is null)
                return null;

            object? argValue = parsed.ArgType switch
            {
                "void"               => null,
                "String" or "string" => Encoding.UTF8.GetString(parsed.Arg),
                "Int"    or "int"    => BitConverter.ToInt32(parsed.Arg),
                "Bool"   or "bool"   => BitConverter.ToBoolean(parsed.Arg),
                _                    => parsed.Arg
            };

            object?[] methodArgs = argValue is null ? [] : [argValue];
            MethodInfo? method = type.GetMethod(
                parsed.MethodName,
                methodArgs.Length == 0
                    ? Type.EmptyTypes
                    : methodArgs.Select(a => a!.GetType()).ToArray()
            );

            if (method is null)
                return null;

            var result = method.Invoke(null, methodArgs);
            return new VMResult(parsed.MethodName, nextToken, result);
        }
    }
}
