using ContainerExpressions.Containers;
using Microsoft.Extensions.Logging;

namespace Std.Out.Core.Services
{
    public interface IDynamodbStorage
    {
        Task<Response<Unit>> Store(string path, string actionPath, string correlationId, string table, string partitionKeyName, string sortKeyName, string timeToLiveName, int timeToLiveHours);
    }

    public sealed class DynamodbStorage(
        ILogger<DynamodbStorage> _log, IDynamodbService _db
        ) : IDynamodbStorage
    {
        public async Task<Response<Unit>> Store(
            string path, string actionPath, string correlationId,
            string table, string partitionKeyName, string sortKeyName,
            string timeToLiveName, int timeToLiveHours
            )
        {
            var response = new Response<Unit>();
            var attributes = new Dictionary<string, string>();

            attributes[partitionKeyName] = path;
            if (!string.Empty.Equals(sortKeyName))
            {
                attributes[partitionKeyName] = path[..^actionPath.Length];
                attributes[sortKeyName] = actionPath;

                if (attributes[partitionKeyName].EndsWith('/')) attributes[partitionKeyName] = attributes[partitionKeyName][..^1];
                if (attributes[sortKeyName].StartsWith('/')) attributes[sortKeyName] = attributes[sortKeyName][1..];
            }

            attributes["correlationId"] = correlationId;

            var ttlName = string.Empty;
            var ttl = 0L;
            if (!string.Empty.Equals(timeToLiveName) && timeToLiveHours > 0)
            {
                ttlName = timeToLiveName;
                ttl = DateTimeOffset.UtcNow.AddHours(timeToLiveHours).ToUnixTimeSeconds();
            }

            try
            {
                response = await _db.Put(table, attributes, ttlName, ttl);
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    _log.LogError(e, "An error occurred storing the correlation Id to DynamoDB [{Table}] ({Path}): {CorrelationId}. {Message}", table, path, correlationId, e.Message);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "An error occurred storing the correlation Id to DynamoDB [{Table}] ({Path}): {CorrelationId}. {Message}", table, path, correlationId, ex.Message);
            }

            return response;
        }
    }
}
