namespace Std.Out.Core.Models.Config
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

    /// <summary>Creates a deterministic key from the merged parameters, in order to query the lastest correlation Id.</summary>
    public sealed class StorageKeyOptions
    {
        /// <summary>A name to represent the program / service storing the correlation Id (i.e. customer_service).</summary>
        public string Application { get; set; }

        /// <summary>The stage the request is running in (i.e. uat).</summary>
        public string Environment { get; set; }

        /// <summary>Pulls the action from the top level calling method defined in the namespace.</summary>
        public string Namespace { get; set; }

        /// <summary>Offset may be used to go down a function call in the namespace (i.e. to skip middleware etc).</summary>
        public int? Offset { get; set; }
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
}
