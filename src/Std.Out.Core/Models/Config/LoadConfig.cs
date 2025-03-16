namespace Std.Out.Core.Models.Config
{
    internal sealed class LoadConfig
    {
        public const string SECTION_NAME = "Load";

        public Dictionary<string, LoadSourceModel> Sources { get; set; }
        public LoadSourceModel Defaults { get; set; }
    }

    internal sealed class LoadSourceModel
    {
        public DisplayType Display { get; set; }
        public StdOutOptions StdOut { get; set; }
    }
}
