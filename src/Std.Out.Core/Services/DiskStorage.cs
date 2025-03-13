using ContainerExpressions.Containers;
using Microsoft.Extensions.Logging;
using Std.Out.Core.Models;
using System.Text.Json;

namespace Std.Out.Core.Services
{
    public interface IDiskStorage
    {
        Task<Response<Unit>> Store(string path, CorrelationDto dto);
        Task<Response<Either<CorrelationDto, NotFound>>> Load(string path);
        Task<Response<string[]>> Query(string path);
    }

    public sealed class DiskStorage(
        ILogger<DiskStorage> _log
        ) : IDiskStorage
    {
        public async Task<Response<Unit>> Store(string path, CorrelationDto dto)
        {
            var response = new Response<Unit>();

            try
            {
                var json = JsonSerializer.Serialize(dto, CoreConstants.JsonOptions);

                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                await File.WriteAllTextAsync(path, json);
                response = response.With(Unit.Instance);
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    _log.LogError(e, "An error occurred storing the correlation Id to Disk ({Path}): {CorrelationId}. {Message}", path, dto.CorrelationId, e.Message);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "An error occurred storing the correlation Id to Disk ({Path}): {CorrelationId}. {Message}", path, dto.CorrelationId, ex.Message);
            }

            return response;
        }

        public async Task<Response<Either<CorrelationDto, NotFound>>> Load(string path)
        {
            var response = new Response<Either<CorrelationDto, NotFound>>();

            try
            {
                if (!File.Exists(path)) response = response.With(new NotFound());
                else
                {
                    var json = await File.ReadAllTextAsync(path);
                    var dto = JsonSerializer.Deserialize<CorrelationDto>(json, CoreConstants.JsonOptions);
                    response = response.With(dto);
                }
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    _log.LogError(e, "An error occurred loading the correlation Id from Disk ({Path}): {Message}", path, e.Message);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "An error occurred loading the correlation Id from Disk ({Path}): {Message}", path, ex.Message);
            }

            return response;
        }

        public Task<Response<string[]>> Query(string path)
        {
            var response = new Response<string[]>();

            try
            {
                var root = path + "/";
                var files = Directory.GetFiles(root, "correlation.json", SearchOption.AllDirectories);
                var actions = files.Select(x => x.Replace('\\', '/')[root.Length..][..^"/correlation.json".Length]).ToArray();
                response = response.With(actions);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "An error occurred querying the key from Disk ({Path}): {Message}", path, ex.Message);
            }

            return Task.FromResult(response);
        }
    }
}
