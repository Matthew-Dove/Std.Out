using Std.Out.Cli.Commands;
using Std.Out.Cli.Services;
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
            Action<LoadConfig> load = null
            )
        {
            if (cw != null) services.Configure(cw);
            if (s3 != null) services.Configure(s3);
            if (db != null) services.Configure(db);
            if (load != null) services.Configure(load);

            // Services for stdout's nuget package.
            services = services
                .AddStdOutServices(options)
                .AddSingleton<ICloudWatchService, CloudWatchService>()
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
