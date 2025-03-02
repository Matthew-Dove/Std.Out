using ContainerExpressions.Containers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Std.Out.Services
{
    internal static class Util
    {
        public static string GetCaller(string @namespace, int offset)
        {
            var caller = string.Empty;
            var callers = new List<(string Namespace, string Class, string Method)>();

            foreach (var frame in new StackTrace(false).GetFrames())
            {
                var method = frame.GetMethod();
                var type = method?.DeclaringType;

                if (!(
                    type == null ||
                    type.Name.Contains('<') ||
                    !type.Namespace.StartsWith(@namespace) ||
                    type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length > 0
                    ))
                {
                    callers.Add((type.Namespace, type.Name, method.Name));
                }
            }

            if (callers.Count - offset > 0)
            {
                var callerInfo = callers[callers.Count - offset - 1];
                caller = $"{callerInfo.Namespace}.{callerInfo.Class}.{callerInfo.Method}";
            }

            return caller.ThrowIfNullOrEmpty("Unable to determine the calling method from namespace: {Namespace}, with offset: {Offset}.".WithArgs(@namespace, offset));
        }
    }
}
