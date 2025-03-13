using ContainerExpressions.Containers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Std.Out.Services
{
    internal static class Util
    {
        private static string _ns = string.Empty;
        private static readonly string[] _excludedNamespaces = [
            "Std.Out",
            "System.",
            "Microsoft.",
            "MS.",
            "Castle.",
            "DynamicProxyGenAssembly",
            "<"
        ];

        public static string GetCallerNamespace()
        {
            static string FindNamespace()
            {
                var stackTrace = new StackTrace();

                for (int i = 0; i < stackTrace.FrameCount; i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame.GetMethod();
                    var declaringType = method.DeclaringType;
                    var ns = declaringType?.Namespace;

                    if (
                        ns != null &&
                        !_excludedNamespaces.Any(ns.StartsWith) &&
                        !method.Name.Contains('<') &&
                        !declaringType.Name.Contains('<') &&
                        declaringType.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length == 0
                        )
                    {
                        _ns = ns;
                        return ns;
                    }
                }

                return string.Empty;
            }

            var caller = string.IsNullOrEmpty(_ns) ? FindNamespace() : _ns;
            return caller.ThrowIfNullOrEmpty("Unable to determine the calling method's namespace.");
        }

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
