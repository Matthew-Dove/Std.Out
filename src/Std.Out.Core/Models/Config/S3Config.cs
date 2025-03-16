namespace Std.Out.Core.Models.Config
{
    internal sealed class S3Config
    {
        public const string SECTION_NAME = "S3";

        public Dictionary<string, S3SourceModel> Sources { get; set; }
        public S3SourceModel Defaults { get; set; }
    }

    internal sealed class S3SourceModel
    {
        public DisplayType Display { get; set; }
        public string Bucket { get; set; }
        public string Prefix { get; set; }
        public string ContentType { get; set; }
        public string[] Files { get; set; }
    }
}
