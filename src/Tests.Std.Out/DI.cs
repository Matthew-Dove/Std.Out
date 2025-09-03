using FrameworkContainers.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Std.Out.Core.Models.Config;
using Tests.Std.Out.Config;

namespace Tests.Std.Out
{
    internal static class DI
    {
        public static T Get<T>() where T : class => (T)_sp.GetService(typeof(T));

        private static readonly IServiceProvider _sp = Build();

        private static IServiceProvider Build()
        {
            return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile(Path.GetFullPath("../../../appsettings.debug.json"), optional: true);
            })
            .ConfigureLogging((context, logging) =>
            {
                logging
                .ClearProviders()
                .AddDebug()
                .SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices((context, services) =>
            {
                services
                .Configure<StorageKeyConfig>(context.Configuration.GetSection(StorageKeyConfig.SECTION_NAME));

                var options = new StdConfigOptions();
                context.Configuration.GetSection(StdConfigOptions.SECTION_NAME).Bind(options);

                services
                .AddStdOutServices(
                    opt => {
                        opt.Sources = options.Sources;
                        opt.Key = options.Key;
                    }
                );
            })
            .Build().Services.AddContainerExpressionsLogging();
        }
    }

    internal static class DEI
    {
        public static T Get<T>() where T : class => (T)_sp.GetService(typeof(T));
        public static IServiceScope GetScope() => _sp.CreateScope();

        private static readonly IServiceProvider _sp = Build();

        private static IServiceProvider Build()
        {
            var builder = Host.CreateApplicationBuilder();

            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile(Path.GetFullPath("../../../appsettings.debug.json"), optional: true)
                ;

            var options = new StdConfigOptions();
            builder.Configuration.GetSection(StdConfigOptions.SECTION_NAME).Bind(options);

            builder.Services
                .Configure<StorageKeyConfig>(builder.Configuration.GetSection(StorageKeyConfig.SECTION_NAME))
                .Configure<CloudWatchConfig>(builder.Configuration.GetSection($"StdCli:{CloudWatchConfig.SECTION_NAME}"))
                .Configure<S3Config>(builder.Configuration.GetSection($"StdCli:{S3Config.SECTION_NAME}"))
                .Configure<DynamodbConfig>(builder.Configuration.GetSection($"StdCli:{DynamodbConfig.SECTION_NAME}"))
                .Configure<LoadConfig>(builder.Configuration.GetSection($"StdCli:{LoadConfig.SECTION_NAME}"))
                ;

            builder.Services
            .AddStdCliServices(
                opt => {
                    opt.Sources = options.Sources;
                    opt.Key = options.Key;
                }
            );

            builder.Logging
                .ClearProviders()
                .AddDebug()
                .SetMinimumLevel(LogLevel.Information)
                ;

            var host = builder.Build();
            host.Services.AddContainerExpressionsLogging();
            return host.Services;
        }
    }

    internal static class DeiExtensions
    {
        public static T Get<T>(this IServiceScope scope) where T : class => (T)scope.ServiceProvider.GetService(typeof(T));
    }
}
