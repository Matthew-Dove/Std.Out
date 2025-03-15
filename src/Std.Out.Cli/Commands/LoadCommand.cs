using ContainerExpressions.Containers;
using Std.Out.Cli.Models;
using Std.Out.Core.Models.Config;
using Std.Out.Core.Models;
using Microsoft.Extensions.Options;
using Std.Out.Core.Services;
using Std.Out.Cli.Services;

namespace Std.Out.Cli.Commands
{
    public interface ILoadCommand
    {
        Task<Response<Either<BadRequest, Unit>>> Execute(CommandModel command);
    }

    public sealed class LoadCommand(
        IOptions<LoadConfig> _config, IStdOut _service, IDisplayService _display
        ) : ILoadCommand
    {
        private static readonly StdOutOptionsDisk _diskSource = new StdOutOptionsDisk { RootPath = string.Empty };
        private static readonly StdOutOptionsS3 _s3Source = new StdOutOptionsS3 { Bucket = string.Empty, Prefix = string.Empty };
        private static readonly StdOutOptionsDynamodb _dynamodbSource = new StdOutOptionsDynamodb { TableName = string.Empty, PartitionKeyName = string.Empty, SortKeyName = string.Empty };

        public async Task<Response<Either<BadRequest, Unit>>> Execute(CommandModel command)
        {
            var response = new Response<Either<BadRequest, Unit>>();

            var src = GetSourceModel(command.SettingsKey, _config.Value);
            if (!src) return response.With(new BadRequest());
            var source = src.Value;

            var stdKey = BuildStdKey(source.StdOut.Key, command.Action);
            var stdConfig = BuildStdConfig(source.StdOut.Sources, Operations.Store | Operations.Query);

            var load = await _service.Load(stdKey, stdConfig);

            if (load)
            {
                if (load.Value.TryGetT1(out var correlationId))
                {
                    _display.Show(source.Display, stdKey.ToString(), correlationId);
                    response = response.With(Unit.Instance);
                }
                else response = response.With(new BadRequest()).LogValue("Correlation Id not found.");
            }

            return response;
        }

        internal static Response<LoadSourceModel> GetSourceModel(string key, LoadConfig config)
        {
            var response = new Response<LoadSourceModel>();
            var model = new LoadSourceModel();
            var @default = config.Defaults;
            var source = config.Sources.GetValueOrDefault(key, default);
            if (source == default) source = config.Sources.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
            if (source == default) return response.LogErrorValue("Load source [{Key}] not found.".WithArgs(key));

            // Merge source with default model.
            model.Display = source.Display == DisplayType.NotSet ? @default.Display : source.Display;
            model.StdOut = source.StdOut == null ? @default?.StdOut : source.StdOut;
            if (source.StdOut != null)
            {
                model.StdOut.Key = source.StdOut.Key == null ? @default?.StdOut?.Key : source.StdOut.Key;
                if (source.StdOut.Key != null)
                {
                    model.StdOut.Key.Application = string.IsNullOrWhiteSpace(source.StdOut.Key.Application) ? @default?.StdOut?.Key?.Application : source.StdOut.Key.Application;
                    model.StdOut.Key.Environment = string.IsNullOrWhiteSpace(source.StdOut.Key.Environment) ? @default?.StdOut?.Key?.Environment : source.StdOut.Key.Environment;
                    model.StdOut.Key.User = string.IsNullOrWhiteSpace(source.StdOut.Key.User) ? @default?.StdOut?.Key?.User : source.StdOut.Key.User;
                }
                model.StdOut.Sources = source.StdOut.Sources == null ? @default?.StdOut?.Sources : source.StdOut.Sources;
                if (source.StdOut.Sources != null)
                {
                    model.StdOut.Sources.Disk = source.StdOut.Sources.Disk == null ? @default?.StdOut?.Sources?.Disk : source.StdOut.Sources.Disk;
                    if (source.StdOut.Sources.Disk != null)
                    {
                        model.StdOut.Sources.Disk.RootPath = string.IsNullOrWhiteSpace(source.StdOut.Sources.Disk.RootPath) ? @default?.StdOut?.Sources?.Disk?.RootPath : source.StdOut.Sources.Disk.RootPath;
                    }
                    model.StdOut.Sources.S3 = source.StdOut.Sources.S3 == null ? @default?.StdOut?.Sources?.S3 : source.StdOut.Sources.S3;
                    if (source.StdOut.Sources.S3 != null)
                    {
                        model.StdOut.Sources.S3.Bucket = string.IsNullOrWhiteSpace(source.StdOut.Sources.S3.Bucket) ? @default?.StdOut?.Sources?.S3?.Bucket : source.StdOut.Sources.S3.Bucket;
                        model.StdOut.Sources.S3.Prefix = string.IsNullOrWhiteSpace(source.StdOut.Sources.S3.Prefix) ? @default?.StdOut?.Sources?.S3?.Prefix : source.StdOut.Sources.S3.Prefix;
                    }
                    model.StdOut.Sources.DynamoDb = source.StdOut.Sources.DynamoDb == null ? @default?.StdOut?.Sources?.DynamoDb : source.StdOut.Sources.DynamoDb;
                    if (source.StdOut.Sources.DynamoDb != null)
                    {
                        model.StdOut.Sources.DynamoDb.TableName = string.IsNullOrWhiteSpace(source.StdOut.Sources.DynamoDb.TableName) ? @default?.StdOut?.Sources?.DynamoDb?.TableName : source.StdOut.Sources.DynamoDb.TableName;
                        model.StdOut.Sources.DynamoDb.PartitionKeyName = string.IsNullOrWhiteSpace(source.StdOut.Sources.DynamoDb.PartitionKeyName) ? @default?.StdOut?.Sources?.DynamoDb?.PartitionKeyName : source.StdOut.Sources.DynamoDb.PartitionKeyName;
                        model.StdOut.Sources.DynamoDb.SortKeyName = string.IsNullOrWhiteSpace(source.StdOut.Sources.DynamoDb.SortKeyName) ? @default?.StdOut?.Sources?.DynamoDb?.SortKeyName : source.StdOut.Sources.DynamoDb.SortKeyName;
                    }
                }
            }

            // Validate source model.
            var isValid = true;
            isValid = isValid && model.StdOut != null;
            isValid = isValid && model.StdOut.Key != null;
            isValid = isValid && !string.IsNullOrWhiteSpace(model.StdOut.Key.Application);
            isValid = isValid && model.StdOut.Sources != null;
            if (model.StdOut.Sources.Disk != null)
            {
                isValid = isValid && !string.IsNullOrWhiteSpace(model.StdOut.Sources.Disk.RootPath);
            }
            if (model.StdOut.Sources.S3 != null)
            {
                isValid = isValid && !string.IsNullOrWhiteSpace(model.StdOut.Sources.S3.Bucket);
            }
            if (model.StdOut.Sources.DynamoDb != null)
            {
                isValid = isValid && !string.IsNullOrWhiteSpace(model.StdOut.Sources.DynamoDb.TableName);
                isValid = isValid && !string.IsNullOrWhiteSpace(model.StdOut.Sources.DynamoDb.PartitionKeyName);
            }
            isValid = isValid && (model.StdOut.Sources.Disk != null || model.StdOut.Sources.S3 != null || model.StdOut.Sources.DynamoDb != null);

            if (isValid)
            {
                // Align optional fields.
                if (model.Display == DisplayType.NotSet) model.Display = DisplayType.Console;
                if (string.IsNullOrWhiteSpace(model.StdOut.Key.Environment)) model.StdOut.Key.Environment = string.Empty;
                if (string.IsNullOrWhiteSpace(model.StdOut.Key.User)) model.StdOut.Key.User = string.Empty;
                if (model.StdOut.Sources.Disk == null) model.StdOut.Sources.Disk = _diskSource;
                if (model.StdOut.Sources.S3 == null) model.StdOut.Sources.S3 = _s3Source;
                if (string.IsNullOrWhiteSpace(model.StdOut.Sources.S3.Prefix)) model.StdOut.Sources.S3.Prefix = string.Empty;
                if (model.StdOut.Sources.DynamoDb == null) model.StdOut.Sources.DynamoDb = _dynamodbSource;
                if (string.IsNullOrWhiteSpace(model.StdOut.Sources.DynamoDb.SortKeyName)) model.StdOut.Sources.DynamoDb.SortKeyName = string.Empty;

                response = response.With(model);
            }
            else
            {
                isValid.LogErrorValue("The merged Load source values are not valid for key: [{Key}].".WithArgs(key));
            }

            return response;
        }

        internal static StorageKey BuildStdKey(StdOutOptionsKey source, string action = null)
        {
            var key = default(StorageKey);
            var app = source.Application;
            var env = source.Environment;
            var usr = source.User;

            if (action == null)
            {
                if (env != string.Empty && usr != string.Empty) key = StorageKey.CreateWithEnvironmentAndUser(app, env, usr);
                else if (usr != string.Empty) key = StorageKey.CreateWithUser(app, usr);
                else if (env != string.Empty) key = StorageKey.CreateWithEnvironment(app, env);
                else key = StorageKey.Create(app);
            }
            else
            {
                if (env != string.Empty && usr != string.Empty) key = StorageKey.CreateWithEnvironmentAndUser(app, env, usr, action);
                else if (usr != string.Empty) key = StorageKey.CreateWithUser(app, usr, action);
                else if (env != string.Empty) key = StorageKey.CreateWithEnvironment(app, env, action);
                else key = StorageKey.Create(app, action);
            }

            return key;
        }

        internal static StdConfig BuildStdConfig(StdOutOptionsSources source, Operations dissect = Operations.None)
        {
            var config = default(StdConfig);
            var dk = default(DiskStdConfig);
            var s3 = default(S3StdConfig);
            var db = default(DynamoDbStdConfig);

            if (source.Disk.RootPath != string.Empty)
            {
                dk = new DiskStdConfig { OperationDissect = dissect, RootPath = source.Disk.RootPath };
            }
            if (source.S3.Bucket != string.Empty)
            {
                s3 = new S3StdConfig { OperationDissect = dissect, Bucket = source.S3.Bucket, Prefix = source.S3.Prefix };
            }
            if (source.DynamoDb.TableName != string.Empty)
            {
                db = new DynamoDbStdConfig { OperationDissect = dissect, TableName = source.DynamoDb.TableName, PartitionKeyName = source.DynamoDb.PartitionKeyName, SortKeyName = source.DynamoDb.SortKeyName };
            }

            if (dk != null && s3 != null && db != null) config = new StdConfig(dk, s3, db);
            else if (dk != null && db != null) config = new StdConfig(dk, db);
            else if (dk != null && s3 != null) config = new StdConfig(dk, s3);
            else if (dk != null) config = new StdConfig(dk);
            else if (s3 != null && db != null) config = new StdConfig(s3, db);
            else if (s3 != null) config = new StdConfig(s3);
            else if (db != null) config = new StdConfig(db);

            return config;
        }
    }
}
