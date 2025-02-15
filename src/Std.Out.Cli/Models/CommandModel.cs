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

        // TODO: Add DynamoDB args - PK | SK | INDEX | option to reload pk/sk from the index item.
    }

    public sealed class Verb
    {
        public const string CloudWatch = "cloudwatch";
        public const string S3 = "s3";
        public const string DynamoDB = "dynamodb";
    }

    public sealed class Option
    {
        public const string Key = "--key";
        public const string K = "-k";

        public const string CorrelationId = "--cid";
        public const string C = "-c";
    }
}
