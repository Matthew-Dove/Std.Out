using ContainerExpressions.Containers;
using Microsoft.Extensions.Options;
using Std.Out.Cli.Models;
using Std.Out.Core.Models.Config;
using Std.Out.Core.Services;

namespace Std.Out.Cli.Services
{
    public interface ICommandService
    {
        Task<Response<Either<BadRequest, Unit>>> Execute(string[] args);
    }

    public sealed class CommandService(
        ICommandParser _parser,
        IOptions<CloudWatchConfig> _cloudWatchConfig, ICloudWatchService _cloudWatchService
        ) : ICommandService
    {
        public async Task<Response<Either<BadRequest, Unit>>> Execute(string[] args)
        {
            var response = new Response<Either<BadRequest, Unit>>();
            var cmd = _parser.Parse(args);
            if (!cmd) return response.With(new BadRequest());
            var command = cmd.Value;

            if (Verb.CloudWatch.Equals(command.Verb))
            {
                var source = GetSourceModel(command.SettingsKey, _cloudWatchConfig.Value);
                var logs = await source.BindAsync(x => _cloudWatchService.Query(x, command.CorrelationId));
                if (!source) response = response.With(new BadRequest());

                if (logs)
                {
                    foreach (var log in logs.Value)
                    {
                        Console.WriteLine(log);
                    }
                    response = response.With(Unit.Instance);
                }
            }
            else if (Verb.S3.Equals(command.Verb))
            {
                // TODO: Implement S3 command.
            }
            else if (Verb.DynamoDB.Equals(command.Verb))
            {
                // TODO: Implement DynamoDB command.
            }

            return response;
        }

        private static Response<CloudWatchSourceModel> GetSourceModel(string key, CloudWatchConfig config)
        {
            var response = new Response<CloudWatchSourceModel>();
            var model = new CloudWatchSourceModel();
            var @default = config.Defaults;
            var source = config.Sources.GetValueOrDefault(key, default);
            if (source == default) source = config.Sources.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
            if (source == default) return response.LogErrorValue("CloudWatch source [{Key}] not found.".WithArgs(key));

            // Merge source with default model.
            model.LogGroups = source.LogGroups == null || source.LogGroups.Length == 0 ? @default?.LogGroups : source.LogGroups;
            model.Limit = source.Limit == 0 ? (@default?.Limit ?? 0) : source.Limit;
            model.RelativeHours = source.RelativeHours == 0 ? (@default?.RelativeHours ?? 0) : source.RelativeHours;
            model.IsPresentFieldName = string.IsNullOrWhiteSpace(source.IsPresentFieldName) ? @default?.IsPresentFieldName : source.IsPresentFieldName;
            model.CorrelationIdFieldName = string.IsNullOrWhiteSpace(source.CorrelationIdFieldName) ? @default?.CorrelationIdFieldName : source.CorrelationIdFieldName;
            model.Fields = source.Fields == null || source.Fields.Length == 0 ? @default?.Fields : source.Fields;
            model.Filters = source.Filters == null || source.Filters.Length == 0 ? @default?.Filters : source.Filters;

            // Validate source model.
            var isValid = true;
            isValid = isValid && model.LogGroups != null && model.LogGroups.Length > 0 && model.LogGroups.Count(string.IsNullOrWhiteSpace) == 0;
            if (model.Fields != null && model.Fields.Length > 0) isValid = isValid && model.Fields.Count(string.IsNullOrWhiteSpace) == 0;
            if (model.Filters != null && model.Filters.Length > 0) isValid = isValid && model.Filters.Count(x => string.IsNullOrWhiteSpace(x.Field) || string.IsNullOrWhiteSpace(x.Value)) == 0;

            if (isValid)
            {
                // Align optional fields.
                if (model.Limit == 0) model.Limit = 25;
                if (model.RelativeHours == 0) model.RelativeHours = 1;
                if (string.IsNullOrWhiteSpace(model.IsPresentFieldName)) model.IsPresentFieldName = string.Empty;
                if (string.IsNullOrWhiteSpace(model.CorrelationIdFieldName)) model.CorrelationIdFieldName = string.Empty;
                if (model.Fields == null || model.Fields.Length == 0) model.Fields = ["@timestamp", "@message"];
                if (model.Filters == null || model.Filters.Length == 0) model.Filters = [];

                response = response.With(model);
            }
            else
            {
                isValid.LogErrorValue("The merged CloudWatch source values are not valid for key: [{Key}].".WithArgs(key));
            }

            return response;
        }
    }
}
