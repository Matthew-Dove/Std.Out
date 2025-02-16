using Amazon.S3;
using Amazon.S3.Model;
using ContainerExpressions.Containers;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Std.Out.Core.Services
{
    public interface IS3Service
    {
        Task<Response<string[]>> List(string bucket, string prefix);
        Task<Response<string>> Download(string bucket, string key, string contentType);
    }

    public sealed class S3Service : IS3Service
    {
        private static readonly AmazonS3Client _client = new AmazonS3Client();

        public async Task<Response<string[]>> List(string bucket, string prefix)
        {
            var response = new Response<string[]>();

            try
            {
                var result = await _client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket, Prefix = prefix });
                if (result.HttpStatusCode == HttpStatusCode.OK)
                {
                    var filenames = result.S3Objects.Select(x => x.Key).ToArray();
                    response = response.With(filenames);
                }
                else
                {
                    result.LogErrorValue(x => "Listing objects failed with HTTP code: {HttpCode}, from bucket: {Bucket}, with prefix: {Prefix}.".WithArgs(x.HttpStatusCode, bucket, prefix));
                }
            }
            catch (Exception ex)
            {
                ex.LogError("Error listing objects from bucket: {Bucket}, with prefix: {Prefix}.".WithArgs(bucket, prefix));
            }

            return response;
        }

        public async Task<Response<string>> Download(string bucket, string key, string contentType)
        {
            var response = new Response<string>();

            try
            {
                using var result = await _client.GetObjectAsync(bucket, key);
                if (result.HttpStatusCode == HttpStatusCode.OK)
                {
                    using var sr = new StreamReader(result.ResponseStream, Encoding.UTF8);
                    var contents = await sr.ReadToEndAsync();

                    if ("json".Equals(contentType) || "application/json".Equals(result.Headers.ContentType))
                    {
                        _ = Try.Run(() =>
                        {
                            using var doc = JsonDocument.Parse(contents);
                            contents = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
                        },
                        "Error serializing content type: {ContentType}, for object: {Key}, from bucket: {Bucket}.".WithArgs(contentType, key, bucket));
                    }

                    response = response.With(contents);
                }
                else
                {
                    result.LogErrorValue(x => "Download failed with HTTP code: {HttpCode}, for object: {Key}, from bucket: {Bucket}.".WithArgs(x.HttpStatusCode, key, bucket));
                }
            }
            catch (Exception ex)
            {
                ex.LogError("Error getting object: {Key}, from bucket: {Bucket}.".WithArgs(key, bucket));
            }

            return response;
        }
    }
}
