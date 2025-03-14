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
            .Build().Services.AddStdOutLogging();
        }
    }
}
