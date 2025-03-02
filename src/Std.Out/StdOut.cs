using ContainerExpressions.Containers;
using ContainerExpressions.Expressions;
using Microsoft.Extensions.Logging;
using Std.Out.Core.Services;
using Std.Out.Models;

namespace Std.Out
{
    /// <summary>The Standard Output service allows you to store the last used correlation Id for a particular key.</summary>
    public interface IStdOut
    {
        /// <summary>Save a "key" to storage (Disk, S3, or DynamoDB), with a payload containing the request's correlation Id.</summary>
        /// <param name="config">Settings for data storage services, in order to persist the last used correlation Id.</param>
        /// <param name="key">A deterministic value, which allows you to find a the last used correlation Id, for a particular application, and action.</param>
        /// <param name="correlationId">The unique identifier assigned to this request, that allows you to track logs across multiple services.</param>
        /// <returns>A valid response, if the payload was stored under the configured data source, with the key created from the merged parameters.</returns>
        Task<Response<Unit>> Store(StdConfig config, StorageKey key, string correlationId);
    }

    public sealed class StdOut(
        ILogger<StdOut> _log, IDiskStorage _disk, IS3Storage _s3, IDynamodbStorage _db
        ) : IStdOut
    {
        private static readonly Task<Response<Unit>> _success = Task.FromResult(Unit.ResponseSuccess);

        public async Task<Response<Unit>> Store(StdConfig config, StorageKey key, string correlationId)
        {
            var response = new Response<Unit>();
            if (config == null || key == null || string.IsNullOrWhiteSpace(correlationId))
            {
                _log.LogError("Invalid parameters, stdout service input values cannot be null, or empty.");
                return response;
            }

            try
            {
                Task<Response<Unit>> disk = _success, s3 = _success, db = _success;
                var path = key.ToString();

                // Store, Load, and Query.
                // DDB: PK = $"{app}/{env}/{user}", SK = $"{action}". PK only is: $"{app}/{env}/{user}/{action}" (i.e. same as folder paths for Disk / S3).
                // Query is done by loading all "actions" (i.e. folders / SKs) under the combined key segments: $"{app}/{env}/{user}/*".

                if (config.Disk != null)
                {
                    var fullPath = Path.Combine(config.Disk.RootPath, path).Replace('\\', '/');
                    disk = _disk.Write(fullPath, correlationId);
                }
                if (config.S3 != null)
                {
                    var fullPath = Path.Combine(config.S3.Prefix, path).Replace('\\', '/');
                    if (fullPath.StartsWith('/')) fullPath = fullPath.Substring(1);
                    s3 = _s3.Store(fullPath, correlationId, config.S3.Bucket);
                }
                if (config.DynamoDb != null)
                {
                    string fullPath = path, actionPath = key.ToString(getActionPath: true);
                    db = _db.Store(
                        fullPath, actionPath, correlationId,
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
    }
}
