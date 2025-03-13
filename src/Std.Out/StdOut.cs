using ContainerExpressions.Containers;
using ContainerExpressions.Expressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Std.Out.Core.Models;
using Std.Out.Core.Services;
using Std.Out.Models;
using Std.Out.Services;

namespace Std.Out
{
    /// <summary>The Standard Output service allows you to store, load, and query the last used correlation Id for a particular key; and given source(s).</summary>
    public interface IStdOut
    {
        /// <summary>
        /// Save a "key" to storage (Disk, S3, or DynamoDB), with a payload containing the request's correlation Id.
        /// <para>The options for AddStdOutServices() must be configured in the host's DI in order to use this overload (i.e. to drop the config, and key arguments).</para>
        /// </summary>
        /// <param name="correlationId">The unique identifier assigned to this request, that allows you to track logs across multiple services.</param>
        /// <returns>A valid response, if the payload was stored under the configured data source, with the key created from the merged parameters.</returns>
        Task<Response<Unit>> Store(string correlationId);

        /// <summary>
        /// Save a "key" to storage (Disk, S3, or DynamoDB), with a payload containing the request's correlation Id.
        /// <para>The options for AddStdOutServices() must be configured in the host's DI in order to use this overload (i.e. to drop the config argument).</para>
        /// </summary>
        /// <param name="key">A deterministic value, which allows you to find a the last used correlation Id, for a particular application, and action.</param>
        /// <param name="correlationId">The unique identifier assigned to this request, that allows you to track logs across multiple services.</param>
        /// <returns>A valid response, if the payload was stored under the configured data source, with the key created from the merged parameters.</returns>
        Task<Response<Unit>> Store(StorageKey key, string correlationId);

        /// <summary>Save a "key" to storage (Disk, S3, or DynamoDB), with a payload containing the request's correlation Id.</summary>
        /// <param name="config">Settings for data storage services, in order to persist the last used correlation Id.</param>
        /// <param name="key">A deterministic value, which allows you to find a the last used correlation Id, for a particular application, and action.</param>
        /// <param name="correlationId">The unique identifier assigned to this request, that allows you to track logs across multiple services.</param>
        /// <returns>A valid response, if the payload was stored under the configured data source, with the key created from the merged parameters.</returns>
        Task<Response<Unit>> Store(StdConfig config, StorageKey key, string correlationId);

        /// <summary>
        /// Load a "key" from storage (Disk, S3, or DynamoDB), to get the most recent correlation Id stored for said key.
        /// <para>The options for AddStdOutServices() must be configured in the host's DI in order to use this overload (i.e. to drop the config, and key arguments).</para>
        /// </summary>
        /// <param name="action">The main outcome / objective of this request (i.e. save_customer).</param>
        /// <returns>The most recent correlation Id for the given key, and source(s). Otherwise NotFound, or an invalid response on error.</returns>
        Task<Response<Either<string, NotFound>>> Load(string action);

        /// <summary>
        /// Load a "key" from storage (Disk, S3, or DynamoDB), to get the most recent correlation Id stored for said key.
        /// <para>The options for AddStdOutServices() must be configured in the host's DI in order to use this overload (i.e. to drop the config argument).</para>
        /// </summary>
        /// <param name="key">A deterministic value, which allows you to find a the last used correlation Id, for a particular application, and action.</param>
        /// <returns>The most recent correlation Id for the given key, and source(s). Otherwise NotFound, or an invalid response on error.</returns>
        Task<Response<Either<string, NotFound>>> Load(StorageKey key);

        /// <summary>Load a "key" from storage (Disk, S3, or DynamoDB), to get the most recent correlation Id stored for said key.</summary>
        /// <param name="config">Settings for data storage services, to retrieve the last used correlation Id.</param>
        /// <param name="key">A deterministic value, which allows you to find a the last used correlation Id, for a particular application, and action.</param>
        /// <returns>The most recent correlation Id for the given key, and source(s). Otherwise NotFound, or an invalid response on error.</returns>
        Task<Response<Either<string, NotFound>>> Load(StdConfig config, StorageKey key);

        /// <summary>
        /// Query a "key" (without an action) from storage (Disk, S3, or DynamoDB), to get all actions using the application, environment, and user prefixes.
        /// <para>The options for AddStdOutServices() must be configured in the host's DI in order to use this overload (i.e. to drop the config argument, and key arguments).</para>
        /// </summary>
        /// <returns>0 to many keys, that can be used in the Load() method, to gather the related correlation Ids.</returns>
        Task<Response<StorageKey[]>> Query();

        /// <summary>
        /// Query a "key" (without an action) from storage (Disk, S3, or DynamoDB), to get all actions using the application, environment, and user prefixes.
        /// <para>The options for AddStdOutServices() must be configured in the host's DI in order to use this overload (i.e. to drop the config argument).</para>
        /// </summary>
        /// <param name="key">A deterministic value, which allows you to find a the last used correlation Id, for a particular application.</param>
        /// <returns>0 to many keys, that can be used in the Load() method, to gather the related correlation Ids.</returns>
        Task<Response<StorageKey[]>> Query(StorageKey key);

        /// <summary>Query a "key" (without an action) from storage (Disk, S3, or DynamoDB), to get all actions using the application, environment, and user prefixes.</summary>
        /// <param name="config">Settings for data storage services, to retrieve the last used correlation Id.</param>
        /// <param name="key">A deterministic value, which allows you to find a the last used correlation Id, for a particular application.</param>
        /// <returns>0 to many keys, that can be used in the Load() method, to gather the related correlation Ids.</returns>
        Task<Response<StorageKey[]>> Query(StdConfig config, StorageKey key);
    }

    public sealed class StdOut(
        ILogger<StdOut> _log, IDiskStorage _disk, IS3Storage _s3, IDynamodbStorage _db, IOptions<StdConfigOptions> _options
        ) : IStdOut
    {
        private static readonly Task<Response<Unit>> _store = Task.FromResult(Unit.ResponseSuccess);
        private static readonly Task<Response<Either<CorrelationDto, NotFound>>> _load = Task.FromResult(Response.Create(new Either<CorrelationDto, NotFound>(new NotFound())));
        private static readonly Task<Response<string[]>> _query = Task.FromResult(Response.Create(Array.Empty<string>()));

        private static StdConfig GetConfig(StdSourceOptions options)
        {
            StdConfig config = null;
            var disk = options?.Disk;
            var s3 = options?.S3;
            var db = options?.DynamoDb;

            if (disk != null && s3 != null && db != null) config = new StdConfig(disk, s3, db);
            else if (disk != null && db != null) config = new StdConfig(disk, db);
            else if (disk != null && s3 != null) config = new StdConfig(disk, s3);
            else if (disk != null) config = new StdConfig(disk);
            else if (s3 != null && db != null) config = new StdConfig(s3, db);
            else if (s3 != null) config = new StdConfig(s3);
            else if (db != null) config = new StdConfig(db);

            return config;
        }

        private static StorageKey GetKey(StorageKeyOptions options, string namespaceOverride = null, string actionOverride = null, bool dropAction = false)
        {
            StorageKey key = null;
            var app = options?.Application;
            var env = options?.Environment;
            var @namespace = options?.Namespace;
            var offset = options?.Offset ?? 0;

            if (string.IsNullOrEmpty(@namespace)) @namespace = namespaceOverride;

            var action = new Either<string, (string Namespace, int Offset)>((@namespace, offset));
            if (actionOverride != null) action = actionOverride;

            if (!string.IsNullOrEmpty(env)) key = StorageKey.CreateWithEnvironment(app, env, action);
            else key = StorageKey.Create(app, action);

            if (dropAction)
            {
                if (!string.IsNullOrEmpty(env)) key = StorageKey.CreateWithEnvironment(app, env);
                else key = StorageKey.Create(app);
            }

            return key;
        }

        public async Task<Response<Unit>> Store(string correlationId)
        {
            var @namespace = Util.GetCallerNamespace();
            var config = GetConfig(_options.Value?.Sources);
            var key = GetKey(_options.Value?.Key, namespaceOverride: @namespace);
            return await Store(config, key, correlationId);
        }

        public async Task<Response<Unit>> Store(StorageKey key, string correlationId)
        {
            var config = GetConfig(_options.Value?.Sources);
            return await Store(config, key, correlationId);
        }

        public async Task<Response<Unit>> Store(StdConfig config, StorageKey key, string correlationId)
        {
            var response = new Response<Unit>();
            if (config == null || key == null || !key.HasAction || string.IsNullOrWhiteSpace(correlationId))
            {
                _log.LogError("Invalid input store parameters for the stdout service.");
                return response;
            }

            try
            {
                Task<Response<Unit>> disk = _store, s3 = _store, db = _store;
                var path = key.ToString();
                var dto = new CorrelationDto(correlationId);

                if (config.Disk != null && !config.Disk.OperationDissect.HasFlag(Operations.Store))
                {
                    var fullPath = Path.Combine(config.Disk.RootPath, path).Replace('\\', '/');
                    disk = _disk.Store(fullPath, dto);
                }
                if (config.S3 != null && !config.S3.OperationDissect.HasFlag(Operations.Store))
                {
                    var fullPath = Path.Combine(config.S3.Prefix, path).Replace('\\', '/');
                    if (fullPath.StartsWith('/')) fullPath = fullPath.Substring(1);
                    s3 = _s3.Store(fullPath, dto, config.S3.Bucket, config.S3.PurgeObjectVersions);
                }
                if (config.DynamoDb != null && !config.DynamoDb.OperationDissect.HasFlag(Operations.Store))
                {
                    string fullPath = path, actionPath = key.ToString(getActionPath: true);
                    db = _db.Store(
                        fullPath, actionPath, dto,
                        config.DynamoDb.TableName, config.DynamoDb.PartitionKeyName, config.DynamoDb.SortKeyName,
                        config.DynamoDb.TimeToLiveName, config.DynamoDb.TimeToLiveHours.GetValueOrDefault(0)
                    );
                }

                response = await Expression.FunnelAsync(disk, s3, db, static (_, _, _) => Unit.Instance);
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    _log.LogError(e, "An error occurred storing the correlation Id: {CorrelationId}. {Message}", correlationId, e.Message);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "An error occurred storing the correlation Id: {CorrelationId}. {Message}", correlationId, ex.Message);
            }

            return response;
        }

        public async Task<Response<Either<string, NotFound>>> Load(string action)
        {
            var config = GetConfig(_options.Value?.Sources);
            var key = GetKey(_options.Value?.Key, actionOverride: action);
            return await Load(config, key);
        }

        public async Task<Response<Either<string, NotFound>>> Load(StorageKey key)
        {
            var config = GetConfig(_options.Value?.Sources);
            return await Load(config, key);
        }

        public async Task<Response<Either<string, NotFound>>> Load(StdConfig config, StorageKey key)
        {
            var response = new Response<Either<string, NotFound>>();
            if (config == null || key == null || !key.HasAction)
            {
                _log.LogError("Invalid input load parameters for the stdout service.");
                return response;
            }

            try
            {
                Task<Response<Either<CorrelationDto, NotFound>>> disk = _load, s3 = _load, db = _load;
                var path = key.ToString();

                if (config.Disk != null && !config.Disk.OperationDissect.HasFlag(Operations.Load))
                {
                    var fullPath = Path.Combine(config.Disk.RootPath, path).Replace('\\', '/');
                    disk = _disk.Load(fullPath);
                }
                if (config.S3 != null && !config.S3.OperationDissect.HasFlag(Operations.Load))
                {
                    var fullPath = Path.Combine(config.S3.Prefix, path).Replace('\\', '/');
                    if (fullPath.StartsWith('/')) fullPath = fullPath.Substring(1);
                    s3 = _s3.Load(fullPath, config.S3.Bucket);
                }
                if (config.DynamoDb != null && !config.DynamoDb.OperationDissect.HasFlag(Operations.Load))
                {
                    string fullPath = path, actionPath = key.ToString(getActionPath: true);
                    db = _db.Load(
                        fullPath, actionPath,
                        config.DynamoDb.TableName, config.DynamoDb.PartitionKeyName, config.DynamoDb.SortKeyName
                    );
                }

                response = await Expression.FunnelAsync(disk, s3, db, static (x, y, z) =>
                {
                    var correlationId = new Either<string, NotFound>(new NotFound());

                    var source = new Either<CorrelationDto, NotFound>[] { x, y, z };
                    var where = source.Where(xyz => xyz.TryGetT1(out _));
                    var select = where.Select(x => { x.TryGetT1(out var t1); return t1; });
                    var order = select.OrderByDescending(x => x.Created);
                    var first = order.FirstOrDefault();

                    if (!string.IsNullOrEmpty(first.CorrelationId)) correlationId = first.CorrelationId;
                    return correlationId;
                });
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    _log.LogError(e, "An error occurred loading the correlation Id. {Message}", e.Message);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "An error occurred loading the correlation Id. {Message}", ex.Message);
            }

            return response;
        }

        public async Task<Response<StorageKey[]>> Query()
        {
            var config = GetConfig(_options.Value?.Sources);
            var key = GetKey(_options.Value?.Key, dropAction: true);
            return await Query(config, key);
        }

        public async Task<Response<StorageKey[]>> Query(StorageKey key)
        {
            var config = GetConfig(_options.Value?.Sources);
            return await Query(config, key);
        }

        public async Task<Response<StorageKey[]>> Query(StdConfig config, StorageKey key)
        {
            var response = new Response<StorageKey[]>();
            if (config == null || key == null || key.HasAction || (config.DynamoDb != null && config.DynamoDb.SortKeyName == string.Empty))
            {
                _log.LogError("Invalid input query parameters for the stdout service.");
                return response;
            }

            try
            {
                Task<Response<string[]>> disk = _query, s3 = _query, db = _query;
                var path = key.ToString();

                if (config.Disk != null && !config.Disk.OperationDissect.HasFlag(Operations.Query))
                {
                    var fullPath = Path.Combine(config.Disk.RootPath, path).Replace('\\', '/');
                    disk = _disk.Query(fullPath);
                }
                if (config.S3 != null && !config.S3.OperationDissect.HasFlag(Operations.Query))
                {
                    var fullPath = Path.Combine(config.S3.Prefix, path).Replace('\\', '/');
                    if (fullPath.StartsWith('/')) fullPath = fullPath.Substring(1);
                    s3 = _s3.Query(fullPath, config.S3.Bucket);
                }
                if (config.DynamoDb != null && !config.DynamoDb.OperationDissect.HasFlag(Operations.Query))
                {
                    db = _db.Query(path, config.DynamoDb.TableName, config.DynamoDb.PartitionKeyName, config.DynamoDb.SortKeyName);
                }

                response = await Expression.FunnelAsync(disk, s3, db, (x, y, z) =>
                {
                    var keys = new List<StorageKey>(x.Length + y.Length + z.Length);

                    keys.AddRange(x.Select(xyz => key.WithAction(xyz)));
                    keys.AddRange(y.Select(xyz => key.WithAction(xyz)));
                    keys.AddRange(z.Select(xyz => key.WithAction(xyz)));

                    return keys.Distinct().ToArray();
                });
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    _log.LogError(e, "An error occurred querying for keys: {Message}", e.Message);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "An error occurred querying for keys: {Message}", ex.Message);
            }

            return response;
        }
    }
}
