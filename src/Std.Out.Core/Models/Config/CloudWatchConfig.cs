namespace Std.Out.Core.Models.Config
{
    public sealed class CloudWatchConfig
    {
        public const string SECTION_NAME = "CloudWatch";

        public Dictionary<string, CloudWatchSourceModel> Sources { get; set; }
        public CloudWatchSourceModel Defaults { get; set; }
    }

    public sealed class CloudWatchSourceModel
    {
        public string[] LogGroups { get; set; }
        public int Limit { get; set; }
        public int RelativeHours { get; set; }
        public string IsPresentFieldName { get; set; }
        public string CorrelationIdFieldName { get; set; }
        public string[] Fields { get; set; }
        public FilterModel[] Filters { get; set; }
    }

    public sealed class FilterModel
    {
        public string Field { get; set; }
        public string Value { get; set; }
    }
}
