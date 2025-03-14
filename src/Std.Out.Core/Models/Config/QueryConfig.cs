namespace Std.Out.Core.Models.Config
{
    public sealed class QueryConfig
    {
        public const string SECTION_NAME = "Query";

        public Dictionary<string, QuerySourceModel> Sources { get; set; }
        public QuerySourceModel Defaults { get; set; }
    }

    public sealed class QuerySourceModel
    {
        public DisplayType Display { get; set; }
        public StdOutOptions StdOut { get; set; }
    }
}
