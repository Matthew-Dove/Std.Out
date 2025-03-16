namespace Std.Out.Cli.Models
{
    internal sealed class CliConstants
    {
        /// <summary>
        /// The mask for the correlation Id; used as part of the S3 key path, or as part of an index's pk / sk value.
        /// <para>This special value will be replaced with the correlation id that's passed in as an argument / option to the cli.</para>
        /// </summary>
        public const string CidMask = "<CID>";
    }
}
