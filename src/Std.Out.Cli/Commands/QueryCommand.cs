using ContainerExpressions.Containers;
using Microsoft.Extensions.Options;
using Std.Out.Cli.Models;
using Std.Out.Cli.Services;
using Std.Out.Core.Models;
using Std.Out.Core.Models.Config;
using Std.Out.Core.Services;

namespace Std.Out.Cli.Commands
{
    public interface IQueryCommand
    {
        Task<Response<Either<BadRequest, Unit>>> Execute(CommandModel command);
    }

    public sealed class QueryCommand(
        IOptions<LoadConfig> _config, IStdOut _service, IDisplayService _display
        ) : IQueryCommand
    {
        public async Task<Response<Either<BadRequest, Unit>>> Execute(CommandModel command)
        {
            var response = new Response<Either<BadRequest, Unit>>();

            var src = LoadCommand.GetSourceModel(command.SettingsKey, _config.Value);
            if (!src) return response.With(new BadRequest());
            var source = src.Value;

            var stdKey = BuildStdKey(source.StdOut.Key);
            var stdConfig = LoadCommand.BuildStdConfig(source.StdOut.Sources, Operations.Store | Operations.Load);

            var query = await _service.Query(stdKey, stdConfig);
            if (query)
            {
                var keys = string.Join(Environment.NewLine, query.Value.Select(x => x.ToString()));
                _display.Show(source.Display, stdKey.ToString(), keys);
                query.Value.Length.LogValue(x => "{Count} query match{Plural} found.".WithArgs(x, x == 1 ? "" : "es"));
                response = response.With(Unit.Instance);
            }

            return response;
        }

        private static StorageKey BuildStdKey(StdOutOptionsKey source)
        {
            var key = default(StorageKey);
            var app = source.Application;
            var env = source.Environment;
            var usr = source.User;

            if (env != string.Empty && usr != string.Empty) key = StorageKey.CreateWithEnvironmentAndUser(app, env, usr);
            else if (usr != string.Empty) key = StorageKey.CreateWithUser(app, usr);
            else if (env != string.Empty) key = StorageKey.CreateWithEnvironment(app, env);
            else key = StorageKey.Create(app);

            return key;
        }
    }
}
