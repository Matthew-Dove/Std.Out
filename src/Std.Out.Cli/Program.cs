﻿using ContainerExpressions.Containers;
using FrameworkContainers.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Std.Out.Cli.Commands;
using Std.Out.Core.Models.Config;
using System.Reflection;

namespace Std.Out.Cli
{
    internal class Program
    {
        private const int _success = 0, _error = 1, _validation = 2;

        /**
         * [CloudWatch]
         * cw --key widgets --cid b6408f5a-6893-4fb7-b996-3946371ab57f
         * --key: The name of the configuration in app settings, that defines the log groups to query, and general filter rules.
         * --cid: The Correlation Id to filter the logs by.
         * 
         * [S3]
         * s3 --key assets --cid b6408f5a-6893-4fb7-b996-3946371ab57f
         * --key: The name of the configuration in app settings, that defines the bucket, and path prefix.
         * --cid: The Correlation Id is part of (or all) of the key, that target files are found under the prefix, and correlation id.
         * 
         * [DynamoDB]
         * db --key orders --pk b6408f5a-6893-4fb7-b996-3946371ab57f --sk 2022-01-01
         * --key: The name of the configuration in app settings, that defines the table name, and index to use.
         * --pk: The Partition Key for an item.
         * --sk: The Sort Key for an item. If not provided, all sks found under the pk are returned.
        **/

        static async Task<int> Main(string[] args)
        {
            Try.SetExceptionLogger(Console.Error.WriteLine);
            var code = _error;
            IHost host = null;

            try
            {
                host = BuildHost(args);

                var cmd = host.Services.GetRequiredService<ICommandService>();
                var response = await cmd.Execute(args);

                code = response.Transform(static x => x.Match(static _ => _validation, static _ => _success)).GetValueOrDefault(_error);
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
            var root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Args = args, ContentRootPath = root });

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                var path = Path.GetFullPath("../../../appsettings.debug.json");
                builder.Configuration.AddJsonFile(path, optional: true, reloadOnChange: false);
            }
#endif
            builder.Services.Configure<CloudWatchConfig>(builder.Configuration.GetSection(CloudWatchConfig.SECTION_NAME));
            builder.Services.Configure<S3Config>(builder.Configuration.GetSection(S3Config.SECTION_NAME));

            builder.Services.AddServicesByConvention("Std.Out.Cli", false, "Std.Out", "Std.Out.Core", "Std.Out.Infrastructure");

            var host = builder.Build();
            host.Services.AddContainerExpressionsLogging();

            return host;
        }
    }
}
