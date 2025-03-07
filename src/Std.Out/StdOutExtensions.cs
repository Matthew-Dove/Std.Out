using FrameworkContainers.Infrastructure;
using Std.Out.Infrastructure;
using Std.Out.Models;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class StdOutExtensions
    {
        /// <summary>Adds stdout services to the host's DI container.</summary>
        public static IServiceCollection AddStdOutServices(this IServiceCollection services, Action<StdConfigOptions> options = null)
        {
            if (options != null) services.Configure(options);

            return services
                .AddSingleton<IMarker, Marker>()
                .AddServicesByConvention("Std.Out", false, "Std.Out", "Std.Out.Core", "Std.Out.Infrastructure");
        }

        /// <summary>Adds stdout information, and error logging to the host's logging providers.</summary>
        public static IServiceProvider AddStdOutLogging(this IServiceProvider sp) => sp.AddContainerExpressionsLogging();
    }
}
