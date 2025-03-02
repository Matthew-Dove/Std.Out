namespace Tests.Std.Out.Config
{
    internal sealed class DynamoDbConfig
    {
        public const string SECTION_NAME = "DynamoDb";

        public string TableName { get; set; }
        public string PartitionKeyName { get; set; }
        public string SortKeyName { get; set; }
        public string TimeToLiveName { get; set; }
        public int? TimeToLiveHours { get; set; }
    }
}
