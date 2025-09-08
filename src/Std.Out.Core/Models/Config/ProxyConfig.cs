namespace Std.Out.Core.Models.Config
{
    public sealed class ProxyConfig
    {
        public const string SECTION_NAME = "Proxy";

        public Dictionary<string, ProxySourceModel> Sources { get; set; }
        public ProxySourceModel Defaults { get; set; }
    }

    public readonly record struct ProxyHeader(string Key, string Value);

    public sealed class ProxySourceModel
    {
        public DisplayType Display { get; set; }
        public string Url { get; set; }
        public ProxyHeader[] Headers { get; set; }
    }
}
