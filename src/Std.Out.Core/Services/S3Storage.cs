using ContainerExpressions.Containers;
using Microsoft.Extensions.Logging;

namespace Std.Out.Core.Services
{
    public interface IS3Storage
    {
        Task<Response<Unit>> Store(string path, string correlationId, string bucket);
    }

    public sealed class S3Storage(
        ILogger<S3Storage> _log, IS3Service _s3
        ) : IS3Storage
    {
        public async Task<Response<Unit>> Store(string path, string correlationId, string bucket)
        {
            var response = new Response<Unit>();
            var content = $$"""
                {
                  "correlationId": "{{correlationId}}"
                }
                """;

            try
            {
                response = await _s3.Upload(bucket, path, content);
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    _log.LogError(e, "An error occurred storing the correlation Id to S3 [{Bucket}] ({Path}): {CorrelationId}. {Message}", bucket, path, correlationId, e.Message);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "An error occurred storing the correlation Id to S3 [{Bucket}] ({Path}): {CorrelationId}. {Message}", bucket, path, correlationId, ex.Message);
            }

            return response;
        }
    }
}
