namespace Std.Out.Core.Models.Config
{
    public sealed class LoadConfig
    {
        public const string SECTION_NAME = "Load";

        public Dictionary<string, LoadSourceModel> Sources { get; set; }
        public LoadSourceModel Defaults { get; set; }
    }

    public sealed class LoadSourceModel
    {
        public DisplayType Display { get; set; }
        public StdOutOptions StdOut { get; set; }
    }
}
