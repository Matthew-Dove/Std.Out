using Microsoft.Extensions.DependencyInjection.Extensions;
using Std.Out.Cli.Services;
using Std.Out.Core.Models.Config;
using Std.Out.Core.Services;
using Std.Out.Infrastructure;
using Std.Out.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class StdOutExtensions
    {
        /// <summary>Adds stdout services to the host's DI container.</summary>
        public static IServiceCollection AddStdOutServices(this IServiceCollection services)
        {
            return AddStdOutServices(services, null);
        }

        /// <summary>Adds stdout services to the host's DI container, sets up stdout's config.</summary>
        public static IServiceCollection AddStdOutServices(this IServiceCollection services, Action<StdConfigOptions> options)
        {
            if (options != null) services.Configure(options);

            return services
                .AddSingleton<IMarker, Marker>()
                .AddSingleton<IStdOut, StdOut>()
                .AddSingleton<IDiskStorage, DiskStorage>()
                .AddSingleton<IS3Storage, S3Storage>()
                .AddSingleton<IS3Service, S3Service>()
                .AddSingleton<IDynamodbStorage, DynamodbStorage>()
                .AddSingleton<IDynamodbService, DynamodbService>()
                ;
        }

        /// <summary>Adds stdout services to the host's DI container, sets up stdout's config; and the CLI's config (when provided).</summary>
        public static IServiceCollection AddStdCliServices(
            this IServiceCollection services,
            Action<StdConfigOptions> options = null,
            Action<CloudWatchConfig> cw = null,
            Action<S3Config> s3 = null,
            Action<DynamodbConfig> ddb = null,
            Action<LoadConfig> load = null
            )
        {
            if (cw != null) services.Configure(cw);
            if (s3 != null) services.Configure(s3);
            if (ddb != null) services.Configure(ddb);
            if (load != null) services.Configure(load);

            services = services
                .AddStdOutServices(options)
                .AddSingleton<IStdCli, StdCli>()
                ;

            // Swap out the display service from the CLI, with an implementation that collects the view data.
            services
                .RemoveAll<IDisplayService>()
                .AddScoped<IDisplayService, Collector>()
                ;

            return services;
        }
    }
}
