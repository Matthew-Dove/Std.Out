using ContainerExpressions.Containers;
using Microsoft.Extensions.Options;
using Std.Out.Cli.Models;
using Std.Out.Cli.Services;
using Std.Out.Core.Models.Config;
using Std.Out.Core.Services;

namespace Std.Out.Cli.Commands
{
    public interface IDynamodbCommand
    {
        Task<Response<Either<BadRequest, Unit>>> Execute(CommandModel command);
    }

    public sealed class DynamodbCommand (
        IOptions<DynamodbConfig> _config, IDynamodbService _service, IDisplayService _display
        ) : IDynamodbCommand
    {
        public async Task<Response<Either<BadRequest, Unit>>> Execute(CommandModel command)
        {
            var response = new Response<Either<BadRequest, Unit>>();

            var src = GetSourceModel(command.SettingsKey, _config.Value, command.CorrelationId).Bind(x => Validate(x, command));
            if (!src) return response.With(new BadRequest());
            var source = src.Value;
            var items = new Response<string[]>();

            if (command.CorrelationId != string.Empty) items = await _service.QueryIndex(source);
            else items = await _service.Query(source, command.PartitionKey, command.SortKey);

            if (items)
            {
                foreach (var item in items.Value)
                {
                    _display.Show(source.Display, source.TableName, item);
                }
                items.LogValue(x => "{Count} item{Plural} found.".WithArgs(x.Value.Length, x.Value.Length == 1 ? "" : "(s)"));
                response = response.With(Unit.Instance);
            }

            return response;
        }

        private static Response<DynamodbSourceModel> GetSourceModel(string key, DynamodbConfig config, string correlationId)
        {
            var response = new Response<DynamodbSourceModel>();
            var model = new DynamodbSourceModel();
            var @default = config.Defaults;
            var source = config.Sources.GetValueOrDefault(key, default);
            if (source == default) source = config.Sources.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
            if (source == default) return response.LogErrorValue("Dynamodb source [{Key}] not found.".WithArgs(key));

            // Merge source with default model.
            model.Display = source.Display == DisplayType.NotSet ? @default.Display : source.Display;
            model.TableName = string.IsNullOrWhiteSpace(source.TableName) ? @default?.TableName : source.TableName;
            model.PartitionKeyName = string.IsNullOrWhiteSpace(source.PartitionKeyName) ? @default?.PartitionKeyName : source.PartitionKeyName;
            model.SortKeyName = string.IsNullOrWhiteSpace(source.SortKeyName) ? @default?.SortKeyName : source.SortKeyName;
            model.IndexName = string.IsNullOrWhiteSpace(source.IndexName) ? @default?.IndexName : source.IndexName;
            model.IndexPartitionKeyName = string.IsNullOrWhiteSpace(source.IndexPartitionKeyName) ? @default?.IndexPartitionKeyName : source.IndexPartitionKeyName;
            model.IndexSortKeyName = string.IsNullOrWhiteSpace(source.IndexSortKeyName) ? @default?.IndexSortKeyName : source.IndexSortKeyName;
            model.IndexPartitionKeyMask = string.IsNullOrWhiteSpace(source.IndexPartitionKeyMask) ? @default?.IndexPartitionKeyMask : source.IndexPartitionKeyMask;
            model.IndexSortKeyMask = string.IsNullOrWhiteSpace(source.IndexSortKeyMask) ? @default?.IndexSortKeyMask : source.IndexSortKeyMask;
            model.Projection = (source.Projection == null || source.Projection.Length == 0) ? @default?.Projection : source.Projection;

            // Validate source model.
            var isValid = true;
            isValid = isValid && !string.IsNullOrWhiteSpace(model.TableName);
            isValid = isValid && !string.IsNullOrWhiteSpace(model.PartitionKeyName);
            isValid = isValid && ((source.Projection == null || source.Projection.Length == 0) || source.Projection.Count(string.IsNullOrWhiteSpace) == 0);

            // Optional validation when a correlation Id is supplied.
            if (correlationId != string.Empty)
            {
                isValid = isValid && !string.IsNullOrWhiteSpace(model.IndexName);
                isValid = isValid && !string.IsNullOrWhiteSpace(model.IndexPartitionKeyName);
                isValid = isValid && !string.IsNullOrWhiteSpace(model.IndexPartitionKeyMask);
                isValid = isValid && (
                    (string.IsNullOrWhiteSpace(model.IndexSortKeyName) && string.IsNullOrWhiteSpace(model.IndexSortKeyMask)) ||
                    (!string.IsNullOrWhiteSpace(model.IndexSortKeyName) && !string.IsNullOrWhiteSpace(model.IndexSortKeyMask))
                );
                isValid = isValid && (
                    (model.IndexPartitionKeyMask != null && model.IndexPartitionKeyMask.Contains(CliConstants.CidMask)) ||
                    (model.IndexSortKeyMask != null && model.IndexSortKeyMask.Contains(CliConstants.CidMask))
                );
            }

            if (isValid)
            {
                // Align optional fields.
                if (model.Display == DisplayType.NotSet) model.Display = DisplayType.Console;
                if (string.IsNullOrWhiteSpace(model.SortKeyName)) model.SortKeyName = string.Empty;
                if (string.IsNullOrWhiteSpace(model.IndexName)) model.IndexName = string.Empty;
                if (string.IsNullOrWhiteSpace(model.IndexPartitionKeyName)) model.IndexPartitionKeyName = string.Empty;
                if (string.IsNullOrWhiteSpace(model.IndexSortKeyName)) model.IndexSortKeyName = string.Empty;
                if (string.IsNullOrWhiteSpace(model.IndexPartitionKeyMask)) model.IndexPartitionKeyMask = string.Empty;
                if (string.IsNullOrWhiteSpace(model.IndexSortKeyMask)) model.IndexSortKeyMask = string.Empty;
                if (source.Projection == null || source.Projection.Length == 0) model.Projection = Array.Empty<string>();

                if (correlationId != string.Empty)
                {
                    model.IndexPartitionKeyMask = model.IndexPartitionKeyMask.Replace(CliConstants.CidMask, correlationId);
                    model.IndexSortKeyMask = model.IndexPartitionKeyMask.Replace(CliConstants.CidMask, correlationId);
                }

                response = response.With(model);
            }
            else
            {
                isValid.LogErrorValue("The merged Dynamodb source values are not valid for key: [{Key}].".WithArgs(key));
            }

            return response;
        }

        private static Response<DynamodbSourceModel> Validate(DynamodbSourceModel source, CommandModel command)
        {
            var isValid = true;

            // When --sortkey arg is passed, then there must be sortkey settings.
            if (command.SortKey != string.Empty)
            {
                if (source.SortKeyName == string.Empty)
                {
                    isValid = false;
                    isValid.LogErrorValue("{SortKeyName} is required when providing a --sortkey argument.".WithArgs(nameof(source.SortKeyName)));
                }
            }

            return isValid ? Response<DynamodbSourceModel>.Success(source) : Response<DynamodbSourceModel>.Error;
        }
    }
}
