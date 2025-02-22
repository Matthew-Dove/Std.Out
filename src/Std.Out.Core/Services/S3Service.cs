using Amazon.S3;
using Amazon.S3.Model;
using ContainerExpressions.Containers;
using Std.Out.Core.Models;
using Std.Out.Core.Models.Config;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Std.Out.Core.Services
{
    public interface IS3Service
    {
        Task<Response<string[]>> List(S3SourceModel source);
        Task<Response<string>> Download(S3SourceModel source, string key);
    }

    public sealed class S3Service : IS3Service
    {
        private static readonly AmazonS3Client _client = new AmazonS3Client();

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
                            contents = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
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
    }
}
