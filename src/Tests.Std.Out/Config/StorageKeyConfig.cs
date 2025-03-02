namespace Tests.Std.Out.Config
{
    internal class StorageKeyConfig
    {
        public const string SECTION_NAME = "StorageKey";

        public string Application { get; set; }
        public string Environment { get; set; }
        public string User { get; set; }
        public string Action { get; set; }
        public string Namespace { get; set; }
        public int? Offset { get; set; }
    }
}
