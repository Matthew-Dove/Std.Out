using ContainerExpressions.Containers;
using Microsoft.Extensions.Logging;

namespace Std.Out.Core.Services
{
    public interface IDiskStorage
    {
        Task<Response<Unit>> Write(string path, string correlationId);
    }

    public sealed class DiskStorage(
        ILogger<DiskStorage> _log
        ) : IDiskStorage
    {
        public async Task<Response<Unit>> Write(string path, string correlationId)
        {
            var response = new Response<Unit>();
            var content = $$"""
                {
                  "correlationId": "{{correlationId}}"
                }
                """;

            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                await File.WriteAllTextAsync(path, content);
                response = response.With(Unit.Instance);
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    _log.LogError(e, "An error occurred storing the correlation Id to Disk ({Path}): {CorrelationId}. {Message}", path, correlationId, e.Message);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "An error occurred storing the correlation Id to Disk ({Path}): {CorrelationId}. {Message}", path, correlationId, ex.Message);
            }

            return response;
        }
    }
}
