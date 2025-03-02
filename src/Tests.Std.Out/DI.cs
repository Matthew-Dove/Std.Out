using FrameworkContainers.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
                .Configure<DiskConfig>(context.Configuration.GetSection(DiskConfig.SECTION_NAME))
                .Configure<S3Config>(context.Configuration.GetSection(S3Config.SECTION_NAME))
                .Configure<DynamoDbConfig>(context.Configuration.GetSection(DynamoDbConfig.SECTION_NAME))
                .Configure<StorageKeyConfig>(context.Configuration.GetSection(StorageKeyConfig.SECTION_NAME));

                services
                .AddServicesByConvention("Std.Out", false, "Std.Out", "Std.Out.Core", "Std.Out.Infrastructure");
            })
            .Build().Services.AddContainerExpressionsLogging();
        }
    }
}
