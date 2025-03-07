using ContainerExpressions.Containers;
using Microsoft.Extensions.Logging;
using Std.Out.Core.Models;
using System.Text.Json;

namespace Std.Out.Core.Services
{
    public interface IS3Storage
    {
        Task<Response<Unit>> Store(string path, CorrelationDto dto, string bucket);
        Task<Response<Either<CorrelationDto, NotFound>>> Load(string path, string bucket);
    }

    public sealed class S3Storage(
        ILogger<S3Storage> _log, IS3Service _s3
        ) : IS3Storage
    {
        public async Task<Response<Unit>> Store(string path, CorrelationDto dto, string bucket)
        {
            var response = new Response<Unit>();

            try
            {
                var json = JsonSerializer.Serialize(dto, CoreConstants.JsonOptions);
                response = await _s3.Upload(bucket, path, json);
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    _log.LogError(e, "An error occurred storing the correlation Id to S3 [{Bucket}] ({Path}): {CorrelationId}. {Message}", bucket, path, dto.CorrelationId, e.Message);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "An error occurred storing the correlation Id to S3 [{Bucket}] ({Path}): {CorrelationId}. {Message}", bucket, path, dto.CorrelationId, ex.Message);
            }

            return response;
        }

        public async Task<Response<Either<CorrelationDto, NotFound>>> Load(string path, string bucket)
        {
            var response = new Response<Either<CorrelationDto, NotFound>>();

            try
            {
                var download = await _s3.Download(bucket, path);
                if (download)
                {
                    if (download.Value.TryGetT2(out _)) response = response.With(new NotFound());
                    else
                    {
                        _ = download.Value.TryGetT1(out var json);
                        var dto = JsonSerializer.Deserialize<CorrelationDto>(json, CoreConstants.JsonOptions);
                        response = response.With(dto);
                    }
                }
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    _log.LogError(e, "An error occurred storing the correlation Id to S3 [{Bucket}] ({Path}): {Message}", bucket, path, e.Message);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "An error occurred storing the correlation Id to S3 [{Bucket}] ({Path}): {Message}", bucket, path, ex.Message);
            }

            return response;
        }
    }
}
