using FrameworkContainers.Network.HttpCollective;
using Microsoft.Extensions.Options;
using Std.Out.Cli.Core.Commands;
using Std.Out.Cli.Core.Services;
using Std.Out.Core.Models.Config;
using Std.Out.Core.Services;
using Std.Out.Infrastructure;
using Std.Out.Services;
using System.ComponentModel;

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

            services.AddSingleton(sp => sp.GetService<IOptions<StdConfigOptions>>()?.Value ?? new StdConfigOptions());

            // Services for stdout's nuget package.
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
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IServiceCollection AddStdCliServices(
            this IServiceCollection services,
            Action<StdConfigOptions> options = null,
            Action<CloudWatchConfig> cw = null,
            Action<S3Config> s3 = null,
            Action<DynamodbConfig> db = null,
            Action<LoadConfig> load = null,
            Action<ProxyConfig> proxy = null
            )
        {
            if (cw != null) services.Configure(cw);
            if (s3 != null) services.Configure(s3);
            if (db != null) services.Configure(db);
            if (load != null) services.Configure(load);
            if (proxy != null) services.Configure(proxy);

            services.AddScoped(sp => sp.GetService<IOptionsSnapshot<CloudWatchConfig>>()?.Value ?? new CloudWatchConfig());
            services.AddScoped(sp => sp.GetService<IOptionsSnapshot<S3Config>>()?.Value ?? new S3Config());
            services.AddScoped(sp => sp.GetService<IOptionsSnapshot<DynamodbConfig>>()?.Value ?? new DynamodbConfig());
            services.AddScoped(sp => sp.GetService<IOptionsSnapshot<LoadConfig>>()?.Value ?? new LoadConfig());
            services.AddScoped(sp => sp.GetService<IOptionsSnapshot<ProxyConfig>>()?.Value ?? new ProxyConfig());

            // Services for stdout's nuget package.
            services = services
                .AddStdOutServices(options)
                .AddSingleton<ICloudWatchService, CloudWatchService>()
                .AddSingleton(typeof(IHttpResponse<>), typeof(HttpResponse<>))
                ;

            // Services for stdout's cli.
            services = services
                .AddScoped<ICommandService, CommandService>()
                .AddScoped<ICommandParser, CommandParser>()
                .AddScoped<ICloudWatchCommand, CloudWatchCommand>()
                .AddScoped<IS3Command, S3Command>()
                .AddScoped<IDynamodbCommand, DynamodbCommand>()
                .AddScoped<IQueryCommand, QueryCommand>()
                .AddScoped<ILoadCommand, LoadCommand>()
                .AddScoped<IProxyCommand, ProxyCommand>()
                ;

            // Services for the stdout nuget package to interact with the cli.
            services
                .AddScoped<IStdCli, StdCli>()
                .AddScoped<ICollectorService, CollectorService>()
                .AddScoped<IDisplayService, Collector>()
                ;

            return services;
        }
    }
}
