using Microsoft.Extensions.Options;
using Std.Out;
using Std.Out.Models;
using Tests.Std.Out.Config;

namespace Tests.Std.Out
{
    public class StdOutTests
    {
        private readonly IStdOut _std;

        private readonly StdConfig _config;
        private readonly StorageKey _key;
        private readonly string _cid;

        public StdOutTests()
        {
            _std = DI.Get<IStdOut>();

            var disk = DI.Get<IOptions<DiskConfig>>().Value;
            var s3 = DI.Get<IOptions<S3Config>>().Value;
            var db = DI.Get<IOptions<DynamoDbConfig>>().Value;
            var key = DI.Get<IOptions<StorageKeyConfig>>().Value;

            var stdDisk = new DiskStdConfig { RootPath = disk.RootPath };
            var stdS3 = new S3StdConfig { Bucket = s3.Bucket, Prefix = s3.Prefix };
            var stdDb = new DynamoDbStdConfig
            {
                TableName = db.TableName,
                PartitionKeyName = db.PartitionKeyName,
                SortKeyName = db.SortKeyName,
                TimeToLiveName = db.TimeToLiveName,
                TimeToLiveHours = db.TimeToLiveHours
            };

            _config = new StdConfig(stdDisk, stdS3, stdDb);
            _key = StorageKey.CreateWithEnvironment(key.Application, key.Environment, (key.Namespace, key.Offset.GetValueOrDefault(0)));
            _cid = Guid.NewGuid().ToString();
        }

        [Fact]
        public async Task Store()
        {
            var result = await _std.Store(_config, _key, _cid);

            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task Load()
        {
            var action = $"{typeof(StdOutTests).Namespace}.{nameof(StdOutTests)}.{nameof(Store)}";
            var key = StorageKey.CreateWithEnvironment(_key.Application, _key.Environment, action);

            var result = await _std.Load(_config, key);

            Assert.True(result.IsValid);
        }
    }
}