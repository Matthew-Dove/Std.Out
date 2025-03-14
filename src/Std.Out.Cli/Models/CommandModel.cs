namespace Std.Out.Cli.Models
{
    public sealed class CommandModel
    {
        /// <summary>The target to query (i.e. CloudWatch, S3, DynamoDB).</summary>
        public string Verb { get; set; }

        /// <summary>The key for the target's configuration in app settings, that defines general values for the aws managed service. </summary>
        public string SettingsKey { get; set; }

        /// <summary>The Correlation Id that groups requests, logs, files, etc together.</summary>
        public string CorrelationId { get; set; }

        /// <summary>The table's PK value to query.</summary>
        public string PartitionKey { get; set; }

        /// <summary>The table's SK value to query.</summary>
        public string SortKey { get; set; }

        /// <summary>The prefix key path of a S3 bucket, to retrieve files from.</summary>
        public string Path { get; set; }
    }

    public sealed class Verb
    {
        public const string CloudWatch = "cloudwatch";
        public const string S3 = "s3";
        public const string DynamoDB = "dynamodb";
        public const string Query = "query";
    }

    public static class Option
    {
        public const string Key = "--key";
        public const string K = "-k";

        public const string CorrelationId = "--cid";
        public const string C = "-c";

        public const string PartitionKey = "--partitionkey";
        public const string Pk = "-pk";

        public const string SortKey = "--sortkey";
        public const string Sk = "-sk";

        public const string Path = "--path";
        public const string P = "-p";
    }

    public static class Flag
    {
        public const string NoLog = "--nolog";
        public const string Nl = "-nl";
    }
}
