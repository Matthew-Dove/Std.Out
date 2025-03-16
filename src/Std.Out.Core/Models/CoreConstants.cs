using System.Text.Json;
using System.Text.Json.Serialization;

namespace Std.Out.Core.Models
{
    internal static class CoreConstants
    {
        /// <summary>A "reasonable" max number to return when the source has too many assets to load.</summary>
        public const int MaxLimit = 100;

        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };
    }
}
