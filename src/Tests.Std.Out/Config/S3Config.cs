namespace Tests.Std.Out.Config
{
    internal sealed class S3Config
    {
        public const string SECTION_NAME = "S3";

        public string Bucket { get; set; }
        public string Prefix { get; set; }
    }
}
