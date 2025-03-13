using ContainerExpressions.Containers;
using System.Drawing;

namespace Std.Out.Models
{
    /// <summary>The configuration settings for the data storage services, that to persist the last used correlation Id for some key.</summary>
    public sealed class StdConfigOptions
    {
        /// <summary>Conventional name to find the config settings for stdout.</summary>
        public const string SECTION_NAME = "StdOut";

        /// <summary>Persistant data storeage options.</summary>
        public StdSourceOptions Sources { get; set; }

        /// <summary>Creates a deterministic key from the merged parameters, in order to query the lastest correlation Id.</summary>
        public StorageKeyOptions Key { get; set; }
    }

    /// <summary>The configuration settings for the data storage services, that to persist the last used correlation Id for some key.</summary>
    public sealed class StdSourceOptions
    {
        /// <summary>Storage configuration for local disk.</summary>
        public DiskStdConfig Disk { get; set; }

        /// <summary>Storage configuration for AWS S3.</summary>
        public S3StdConfig S3 { get; set; }

        /// <summary>Storage configuration for AWS DynamoDB.</summary>
        public DynamoDbStdConfig DynamoDb { get; set; }
    }

    /// <summary>The configuration settings for the data storage services, that to persist the last used correlation Id for some key.</summary>
    public sealed class StdConfig
    {
        /// <summary>Storage configuration for local disk.</summary>
        public DiskStdConfig Disk { get; }

        /// <summary>Storage configuration for AWS S3.</summary>
        public S3StdConfig S3 { get; }

        /// <summary>Storage configuration for AWS DynamoDB.</summary>
        public DynamoDbStdConfig DynamoDb { get; }

        public StdConfig(NotNull<DiskStdConfig> disk) { Disk = disk.ThrowIf(DiskStdConfig.IsNotValid); }
        public StdConfig(NotNull<DiskStdConfig> disk, NotNull<S3StdConfig> s3) { Disk = disk.ThrowIf(DiskStdConfig.IsNotValid); S3 = s3.ThrowIf(S3StdConfig.IsNotValid); }
        public StdConfig(NotNull<DiskStdConfig> disk, NotNull<DynamoDbStdConfig> dynamoDb) { Disk = disk.ThrowIf(DiskStdConfig.IsNotValid); DynamoDb = dynamoDb.ThrowIf(DynamoDbStdConfig.IsNotValid); }
        public StdConfig(NotNull<DiskStdConfig> disk, NotNull<S3StdConfig> s3, NotNull<DynamoDbStdConfig> dynamoDb) { Disk = disk.ThrowIf(DiskStdConfig.IsNotValid); S3 = s3.ThrowIf(S3StdConfig.IsNotValid); DynamoDb = dynamoDb.ThrowIf(DynamoDbStdConfig.IsNotValid); }
        public StdConfig(NotNull<S3StdConfig> s3, NotNull<DynamoDbStdConfig> dynamoDb) { S3 = s3.ThrowIf(S3StdConfig.IsNotValid); DynamoDb = dynamoDb.ThrowIf(DynamoDbStdConfig.IsNotValid); }
        public StdConfig(NotNull<S3StdConfig> s3) { S3 = s3.ThrowIf(S3StdConfig.IsNotValid); }
        public StdConfig(NotNull<DynamoDbStdConfig> dynamoDb) { DynamoDb = dynamoDb.ThrowIf(DynamoDbStdConfig.IsNotValid); }
    }

    /// <summary>Api operations available though the stdout nuget package.</summary>
    [Flags] public enum Operations { None = 0, Store = 1, Load = 2, Query = 4 }

    /// <summary>Storage configuration for local disk.</summary>
    public sealed class DiskStdConfig
    {
        /// <summary>The local disk root path, where the key is used to store the correlation Id.</summary>
        public string RootPath { get; set; }

        /// <summary>Any dissections marked here will not be used when attempting the store, load, or query operations.</summary>
        public Operations OperationDissect { get; set; }

        internal static bool IsNotValid(NotNull<DiskStdConfig> disk) => !IsValid(disk.Value);
        internal static bool IsValid(DiskStdConfig disk) => !string.IsNullOrWhiteSpace(disk.RootPath);
    }

    /// <summary>Storage configuration for AWS S3.</summary>
    public sealed class S3StdConfig
    {
        /// <summary>The name of the bucket in S3, where the key is used to store the correlation Id.</summary>
        public string Bucket { get; set; }

        /// <summary>A prefix to prepend to the key (can be empty).</summary>
        public string Prefix { get => _prefix; set { _prefix = string.IsNullOrWhiteSpace(value) ? string.Empty : value; } }
        private string _prefix;

        /// <summary>When true, removes all existing object versions after uploading a new one.</summary>
        public bool PurgeObjectVersions { get; set; }

        /// <summary>Any dissections marked here will not be used when attempting the store, load, or query operations.</summary>
        public Operations OperationDissect { get; set; }

        internal static bool IsNotValid(NotNull<S3StdConfig> s3) => !IsValid(s3.Value);
        internal static bool IsValid(S3StdConfig s3) => !string.IsNullOrWhiteSpace(s3.Bucket);
    }

    /// <summary>Storage configuration for AWS DynamoDB.</summary>
    public sealed class DynamoDbStdConfig
    {
        /// <summary>The name of the table in DynamoDB, where the key is used to store the correlation Id.</summary>
        public string TableName { get; set; }

        /// <summary>The name of the Partition Key in the DynamoDB table (where the key is stored).</summary>
        public string PartitionKeyName { get; set; }

        /// <summary>The name of the Sort Key in the DynamoDB table (where the key is stored), this is optional depending on the table setup.</summary>
        public string SortKeyName { get => _sortKeyName; set { _sortKeyName = string.IsNullOrWhiteSpace(value) ? string.Empty : value; } }
        private string _sortKeyName;

        /// <summary>The name of TTL attribute for the table, this is optional.</summary>
        public string TimeToLiveName { get; set; }

        /// <summary>The minimum time (in hours) this item should be kept for, this is optional.</summary>
        public int? TimeToLiveHours { get; set; }

        /// <summary>Any dissections marked here will not be used when attempting the store, load, or query operations.</summary>
        public Operations OperationDissect { get; set; }

        internal static bool IsNotValid(NotNull<DynamoDbStdConfig> db) => !IsValid(db.Value);
        internal static bool IsValid(DynamoDbStdConfig db) =>
            !string.IsNullOrWhiteSpace(db.TableName) &&
            !string.IsNullOrWhiteSpace(db.PartitionKeyName) &&
            (string.Empty.Equals(db.TimeToLiveName) || (!string.IsNullOrWhiteSpace(db.TimeToLiveName) && db.TimeToLiveHours is not null)) &&
            (db.TimeToLiveHours is null || db.TimeToLiveHours > 0)
            && (db.TimeToLiveHours is null || db.TimeToLiveHours < 10001);
    }
}
