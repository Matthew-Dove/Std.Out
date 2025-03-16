using ContainerExpressions.Containers;
using Microsoft.Extensions.Logging;
using Std.Out.Core.Models;
using System.Reflection;
using System.Text.Json;

namespace Std.Out.Core.Services
{
    internal interface IDynamodbStorage
    {
        Task<Response<Unit>> Store(string path, string actionPath, CorrelationDto dto, string table, string partitionKeyName, string sortKeyName, string timeToLiveName, int timeToLiveHours);
        Task<Response<Either<CorrelationDto, NotFound>>> Load(string path, string actionPath, string table, string partitionKeyName, string sortKeyName);
        Task<Response<string[]>> Query(string path, string table, string partitionKeyName, string sortKeyName);
    }

    internal sealed class DynamodbStorage(
        ILogger<DynamodbStorage> _log, IDynamodbService _db
        ) : IDynamodbStorage
    {
        public async Task<Response<Unit>> Store(
            string path, string actionPath, CorrelationDto dto,
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

            var properties = ModelToDictionary.Convert(dto);
            foreach (var kv in properties) attributes[kv.Key] = kv.Value;

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
                    _log.LogError(e, "An error occurred storing the correlation Id to DynamoDB [{Table}] ({Path}): {CorrelationId}. {Message}", table, path, dto.CorrelationId, e.Message);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "An error occurred storing the correlation Id to DynamoDB [{Table}] ({Path}): {CorrelationId}. {Message}", table, path, dto.CorrelationId, ex.Message);
            }

            return response;
        }

        public async Task<Response<Either<CorrelationDto, NotFound>>> Load(string path, string actionPath, string table, string partitionKeyName, string sortKeyName)
        {
            var response = new Response<Either<CorrelationDto, NotFound>>();
            string pk = path, sk = string.Empty;

            if (!string.Empty.Equals(sortKeyName))
            {
                pk = path[..^actionPath.Length];
                sk = actionPath;

                if (pk.EndsWith('/')) pk = pk[..^1];
                if (sk.StartsWith('/')) sk = sk[1..];
            }

            try
            {
                var get = await _db.Get(table, partitionKeyName, pk, sortKeyName, sk);
                if (get)
                {
                    if (get.Value.TryGetT2(out _)) response = response.With(new NotFound());
                    else
                    {
                        _ = get.Value.TryGetT1(out var json);
                        var dto = JsonSerializer.Deserialize<CorrelationDto>(json, CoreConstants.JsonOptions);
                        response = response.With(dto);
                    }
                }
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    _log.LogError(e, "An error occurred loading the correlation Id from DynamoDB [{Table}] ({Path}): {Message}", table, path, e.Message);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "An error occurred loading the correlation Id from DynamoDB [{Table}] ({Path}): {Message}", table, path, ex.Message);
            }

            return response;
        }

        public async Task<Response<string[]>> Query(string path, string table, string partitionKeyName, string sortKeyName)
        {
            var response = new Response<string[]>();

            try
            {
                response = await _db.QuerySk(table, partitionKeyName, path, sortKeyName);
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    _log.LogError(e, "An error occurred querying the key from DynamoDB [{Table}] ({Path}): {Message}", table, path, e.Message);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "An error occurred querying the key from DynamoDB [{Table}] ({Path}): {Message}", table, path, ex.Message);
            }

            return response;
        }
    }

    file static class ModelToDictionary
    {
        public static Dictionary<string, string> Convert<T>(T model) => ModelToDictionary<T>.Convert(model);
    }

    file static class ModelToDictionary<T>
    {
        private static readonly PropertyInfo[] _properties = typeof(T).GetProperties().Where(p => p.CanRead).ToArray();

        public static Dictionary<string, string> Convert(T model)
        {
            return _properties.ToDictionary(
                pi => char.ToLowerInvariant(pi.Name[0]) + pi.Name[1..],
                pi => pi.GetValue(model)?.ToString() ?? string.Empty
            );
        }
    }
}
