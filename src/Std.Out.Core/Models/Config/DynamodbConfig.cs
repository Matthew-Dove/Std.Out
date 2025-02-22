namespace Std.Out.Core.Models.Config
{
    public sealed class DynamodbConfig
    {
        public const string SECTION_NAME = "DynamoDb";

        public Dictionary<string, DynamodbSourceModel> Sources { get; set; }
        public DynamodbSourceModel Defaults { get; set; }
    }

    public sealed class DynamodbSourceModel
    {
        public DisplayType Display { get; set; }
        public string TableName { get; set; }
        public string PartitionKeyName { get; set; }
        public string SortKeyName { get; set; }
        public string IndexName { get; set; }
        public string IndexPartitionKeyName { get; set; }
        public string IndexSortKeyName { get; set; }
        public string IndexPartitionKeyMask { get; set; }
        public string IndexSortKeyMask { get; set; }
        public string[] Projection { get; set; }
    }
}
