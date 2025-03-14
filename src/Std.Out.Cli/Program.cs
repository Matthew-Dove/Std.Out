﻿using ContainerExpressions.Containers;
using FrameworkContainers.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Std.Out.Cli.Commands;
using Std.Out.Cli.Models;
using Std.Out.Core.Models.Config;
using System.Diagnostics;
using System.Reflection;

namespace Std.Out.Cli
{
    internal class Program
    {
        private const int _success = 0, _error = 1, _validation = 2;

        static async Task<int> Main(string[] args)
        {
            Try.SetExceptionLogger(Console.Error.WriteLine);
            var code = _error;
            IHost host = null;

            try
            {
                var sw = Stopwatch.GetTimestamp();
                host = BuildHost(args);

                var cmd = host.Services.GetRequiredService<ICommandService>();
                var response = await cmd.Execute(args);
                code = response.Transform(static x => x.Match(static _ => _validation, static _ => _success)).GetValueOrDefault(_error);

                var log = host.Services.GetRequiredService<ILogger<Program>>();
                var elapsed = Stopwatch.GetElapsedTime(sw);
                log.LogInformation("Execution time: {Elapsed}ms.", elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                code = _error;
                ex.LogError("Top level CLI error.");
            }
            finally
            {
                host?.Dispose();
            }

            return code;
        }

        private static IHost BuildHost(string[] args)
        {
            var noLog = args.FirstOrDefault(static x => Flag.NoLog.Equals(x, StringComparison.OrdinalIgnoreCase) || Flag.Nl.Equals(x, StringComparison.OrdinalIgnoreCase)) is not null;
            var root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Args = args, ContentRootPath = root });

#if DEBUG
            if (Debugger.IsAttached)
            {
                var path = Path.GetFullPath("../../../appsettings.debug.json");
                builder.Configuration.AddJsonFile(path, optional: true, reloadOnChange: false);
            }
#endif
            builder.Services.Configure<CloudWatchConfig>(builder.Configuration.GetSection(CloudWatchConfig.SECTION_NAME));
            builder.Services.Configure<S3Config>(builder.Configuration.GetSection(S3Config.SECTION_NAME));
            builder.Services.Configure<DynamodbConfig>(builder.Configuration.GetSection(DynamodbConfig.SECTION_NAME));
            builder.Services.Configure<QueryConfig>(builder.Configuration.GetSection(QueryConfig.SECTION_NAME));
            builder.Services.Configure<LoadConfig>(builder.Configuration.GetSection(LoadConfig.SECTION_NAME));

            builder.Services.AddServicesByConvention("Std.Out.Cli", false, "Std.Out", "Std.Out.Core", "Std.Out.Infrastructure");

            if (noLog) builder.Logging.ClearProviders();
            var host = builder.Build();
            host.Services.AddContainerExpressionsLogging();

            return host;
        }
    }
}
