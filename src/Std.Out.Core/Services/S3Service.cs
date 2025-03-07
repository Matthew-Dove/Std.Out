using Amazon.S3;
using Amazon.S3.Model;
using ContainerExpressions.Containers;
using Std.Out.Core.Models;
using Std.Out.Core.Models.Config;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Std.Out.Core.Services
{
    public interface IS3Service
    {
        Task<Response<string[]>> List(S3SourceModel source);
        Task<Response<string>> Download(S3SourceModel source, string key);
        Task<Response<Either<string, NotFound>>> Download(string bucket, string key);
        Task<Response<Unit>> Upload(string bucket, string key, string content);
    }

    public sealed class S3Service : IS3Service
    {
        private static readonly AmazonS3Client _client = new AmazonS3Client();
        private static readonly ConcurrentDictionary<string, string> _cache = new ConcurrentDictionary<string, string>();

        public async Task<Response<string[]>> List(S3SourceModel source)
        {
            var response = new Response<string[]>();

            try
            {
                var result = await _client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = source.Bucket, Prefix = source.Prefix, MaxKeys = CoreConstants.MaxLimit });
                if (result.HttpStatusCode == HttpStatusCode.OK)
                {
                    var filter = result.S3Objects.Where(x => source.Files.Length == 0 || source.Files.FirstOrDefault(y => x.Key.EndsWith(y, StringComparison.OrdinalIgnoreCase)) != default);
                    var filenames = filter.Select(x => x.Key).ToArray();
                    response = response.With(filenames);
                }
                else
                {
                    result.LogErrorValue(x => "Listing objects failed with HTTP code: {HttpCode}, from bucket: {Bucket}, with prefix: {Prefix}.".WithArgs(x.HttpStatusCode, source.Bucket, source.Prefix));
                }
            }
            catch (Exception ex)
            {
                ex.LogError("Error listing objects from bucket: {Bucket}, with prefix: {Prefix}.".WithArgs(source.Bucket, source.Prefix));
            }

            return response;
        }

        public async Task<Response<string>> Download(S3SourceModel source, string key)
        {
            var response = new Response<string>();

            try
            {
                using var result = await _client.GetObjectAsync(source.Bucket, key);
                if (result.HttpStatusCode == HttpStatusCode.OK)
                {
                    using var sr = new StreamReader(result.ResponseStream, Encoding.UTF8);
                    var contents = await sr.ReadToEndAsync();

                    if ("json".Equals(source.ContentType) || "application/json".Equals(result.Headers.ContentType))
                    {
                        _ = Try.Run(() =>
                        {
                            using var doc = JsonDocument.Parse(contents);
                            contents = JsonSerializer.Serialize(doc.RootElement, CoreConstants.JsonOptions);
                        },
                        "Error serializing content type: {ContentType}, for object: {Key}, from bucket: {Bucket}.".WithArgs(source.ContentType, key, source.Bucket));
                    }

                    response = response.With(contents);
                }
                else
                {
                    result.LogErrorValue(x => "Download failed with HTTP code: {HttpCode}, for object: {Key}, from bucket: {Bucket}.".WithArgs(x.HttpStatusCode, key, source.Bucket));
                }
            }
            catch (Exception ex)
            {
                ex.LogError("Error getting object: {Key}, from bucket: {Bucket}.".WithArgs(key, source.Bucket));
            }

            return response;
        }

        public async Task<Response<Either<string, NotFound>>> Download(string bucket, string key)
        {
            var response = new Response<Either<string, NotFound>>();

            try
            {
                using var result = await _client.GetObjectAsync(bucket, key);
                if (result.HttpStatusCode == HttpStatusCode.OK)
                {
                    using var sr = new StreamReader(result.ResponseStream, Encoding.UTF8);
                    var contents = await sr.ReadToEndAsync();
                    response = response.With(contents);
                }
                else
                {
                    result.LogErrorValue(x => "Download failed with HTTP code: {HttpCode}, for object: {Key}, from bucket: {Bucket}.".WithArgs(x.HttpStatusCode, key, bucket));
                }
            }
            catch (AmazonS3Exception aex) when (aex.StatusCode == HttpStatusCode.NotFound)
            {
                response = response.With(new NotFound());
            }
            catch (Exception ex)
            {
                ex.LogError("Error getting object: {Key}, from bucket: {Bucket}.".WithArgs(key, bucket));
            }

            return response;
        }

        public async Task<Response<Unit>> Upload(string bucket, string key, string content)
        {
            var response = new Response<Unit>();

            try
            {
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
                var put = new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = key,
                    InputStream = ms,
                    ContentType = key.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? "application/json" : "text/plain"
                };

                var result = await _client.PutObjectAsync(put);
                if (result.HttpStatusCode == HttpStatusCode.OK)
                {
                    response = await CleanUpOldVersions(bucket, key);
                }
                else
                {
                    result.LogErrorValue(x => "Upload failed with HTTP code: {HttpCode}, for object: {Key}, to bucket: {Bucket}.".WithArgs(x.HttpStatusCode, key, bucket));
                }
            }
            catch (Exception ex)
            {
                ex.LogError("Error uploading object: {Key}, to bucket: {Bucket}.".WithArgs(key, bucket));
            }

            return response;
        }

        private static async Task<Response<Unit>> CleanUpOldVersions(string bucket, string key)
        {
            var response = Unit.ResponseSuccess;
            if (_cache.ContainsKey(bucket)) return response;

            var isVersioned = await _client.GetBucketVersioningAsync(new GetBucketVersioningRequest { BucketName = bucket });
            if (isVersioned.VersioningConfig.Status != VersionStatus.Enabled)
            {
                _cache.TryAdd(bucket, string.Empty);
                return response;
            }

            var allVersions = await _client.ListVersionsAsync(new ListVersionsRequest { BucketName = bucket, Prefix = key });
            var oldVersions = new List<KeyVersion>();
            foreach (var version in allVersions.Versions)
            {
                if (!version.IsLatest)
                {
                    oldVersions.Add(new KeyVersion { Key = key, VersionId = version.VersionId });
                }
            }

            if (oldVersions.Count > 0)
            {
                var delete = await _client.DeleteObjectsAsync(new DeleteObjectsRequest { BucketName = bucket, Objects = oldVersions });
                if (delete.DeletedObjects.Count != oldVersions.Count) response = Unit.ResponseError;
            }

            return response;
        }
    }
}
