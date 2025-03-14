namespace Std.Out.Core.Models.Config
{
    public sealed class StdOutOptions
    {
        public StdOutOptionsKey Key { get; set; }
        public StdOutOptionsSources Sources { get; set; }
    }

    public sealed class StdOutOptionsKey
    {
        public string Application { get; set; }
        public string Environment { get; set; }
        public string User { get; set; }
    }

    public sealed class StdOutOptionsSources
    {
        public StdOutOptionsDisk Disk { get; set; }
        public StdOutOptionsS3 S3 { get; set; }
        public StdOutOptionsDynamodb DynamoDb { get; set; }
    }

    public sealed class StdOutOptionsDisk
    {
        public string RootPath { get; set; }
    }

    public sealed class StdOutOptionsS3
    {
        public string Bucket { get; set; }
        public string Prefix { get; set; }
    }

    public sealed class StdOutOptionsDynamodb
    {
        public string TableName { get; set; }
        public string PartitionKeyName { get; set; }
        public string SortKeyName { get; set; }
    }
}
