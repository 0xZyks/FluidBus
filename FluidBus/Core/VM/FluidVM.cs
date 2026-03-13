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

        public static VMResult? Run(byte[] token, byte[][]? args = null)
        {
            var (parsed, nextToken) = FluidCoreAPI.ParseBytecodeByToken(token, args);
            if (parsed is null) return null;

            Type? type = Type.GetType(parsed.TypeName)
                ?? AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(parsed.TypeName))
                .FirstOrDefault(t => t != null);

            if (type is null) return null;

            object?[] methodArgs = parsed.Args.Length == 0
                ? []
                : parsed.Args.Select(argBytes => parsed.ArgType switch
                        {
                        "void"               => (object?)null,
                        "String" or "string" => Encoding.UTF8.GetString(argBytes),
                        "Int"    or "int"    => (object?)BitConverter.ToInt32(argBytes),
                        "Bool"   or "bool"   => (object?)BitConverter.ToBoolean(argBytes),
                        _                    => (object?)argBytes
                        }).ToArray();

            Type[] argTypes = methodArgs.Length == 0
                ? Type.EmptyTypes
                : methodArgs.Select(a => a!.GetType()).ToArray();

            MethodInfo? method = type.GetMethod(parsed.MethodName, argTypes);
            if (method is null) return null;

            object?[] invokeArgs = methodArgs.Length == 0 ? [] : methodArgs;
            var result = method.Invoke(null, invokeArgs);
            return new VMResult(parsed.MethodName, nextToken, result);
        }
    }
}
