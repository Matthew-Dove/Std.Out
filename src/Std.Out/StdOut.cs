using ContainerExpressions.Containers;
using ContainerExpressions.Expressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Std.Out.Core.Models;
using Std.Out.Core.Services;
using Std.Out.Models;

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

        //Task<Response<StorageKey[]>> Query(StdConfig config, StdApplication Application, StdEnvironment Environment = null, StdUser User = null);
    }

    public sealed class StdOut(
        ILogger<StdOut> _log, IDiskStorage _disk, IS3Storage _s3, IDynamodbStorage _db, IOptions<StdConfigOptions> _options
        ) : IStdOut
    {
        private static readonly Task<Response<Unit>> _store = Task.FromResult(Unit.ResponseSuccess);
        private static readonly Task<Response<Either<CorrelationDto, NotFound>>> _load = Task.FromResult(Response.Create(new Either<CorrelationDto, NotFound>(new NotFound())));

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

        private static StorageKey GetKey(StorageKeyOptions options, string actionOverride = null)
        {
            StorageKey key = null;
            var app = options?.Application;
            var env = options?.Environment;
            var @namespace = options?.Namespace;
            var offset = options?.Offset ?? 0;

            var action = new Either<string, (string Namespace, int Offset)>((@namespace, offset));
            if (actionOverride != null) action = actionOverride;

            if (!string.IsNullOrEmpty(env)) key = StorageKey.CreateWithEnvironment(app, env, action);
            else key = StorageKey.Create(app, action);

            return key;
        }

        public async Task<Response<Unit>> Store(string correlationId)
        {
            var config = GetConfig(_options.Value?.Sources);
            var key = GetKey(_options.Value?.Key);
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
                _log.LogError("Invalid parameters, stdout service input values cannot be null, or empty.");
                return response;
            }

            try
            {
                Task<Response<Unit>> disk = _store, s3 = _store, db = _store;
                var path = key.ToString();
                var dto = new CorrelationDto(correlationId);

                if (config.Disk != null)
                {
                    var fullPath = Path.Combine(config.Disk.RootPath, path).Replace('\\', '/');
                    disk = _disk.Store(fullPath, dto);
                }
                if (config.S3 != null)
                {
                    var fullPath = Path.Combine(config.S3.Prefix, path).Replace('\\', '/');
                    if (fullPath.StartsWith('/')) fullPath = fullPath.Substring(1);
                    s3 = _s3.Store(fullPath, dto, config.S3.Bucket);
                }
                if (config.DynamoDb != null)
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
            var key = GetKey(_options.Value?.Key, action);
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
                _log.LogError("Invalid parameters, stdout service input values cannot be null, or empty.");
                return response;
            }

            try
            {
                Task<Response<Either<CorrelationDto, NotFound>>> disk = _load, s3 = _load, db = _load;
                var path = key.ToString();

                if (config.Disk != null)
                {
                    var fullPath = Path.Combine(config.Disk.RootPath, path).Replace('\\', '/');
                    disk = _disk.Load(fullPath);
                }
                if (config.S3 != null)
                {
                    var fullPath = Path.Combine(config.S3.Prefix, path).Replace('\\', '/');
                    if (fullPath.StartsWith('/')) fullPath = fullPath.Substring(1);
                    s3 = _s3.Load(fullPath, config.S3.Bucket);
                }
                if (config.DynamoDb != null)
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
    }
}
