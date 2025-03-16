namespace Std.Out.Core.Models.Config
{
    internal sealed class StdOutOptions
    {
        public StdOutOptionsKey Key { get; set; }
        public StdOutOptionsSources Sources { get; set; }
    }

    internal sealed class StdOutOptionsKey
    {
        public string Application { get; set; }
        public string Environment { get; set; }
        public string User { get; set; }
    }

    internal sealed class StdOutOptionsSources
    {
        public StdOutOptionsDisk Disk { get; set; }
        public StdOutOptionsS3 S3 { get; set; }
        public StdOutOptionsDynamodb DynamoDb { get; set; }
    }

    internal sealed class StdOutOptionsDisk
    {
        public string RootPath { get; set; }
    }

    internal sealed class StdOutOptionsS3
    {
        public string Bucket { get; set; }
        public string Prefix { get; set; }
    }

    internal sealed class StdOutOptionsDynamodb
    {
        public string TableName { get; set; }
        public string PartitionKeyName { get; set; }
        public string SortKeyName { get; set; }
    }
}
