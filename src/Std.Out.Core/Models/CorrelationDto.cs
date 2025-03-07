namespace Std.Out.Core.Models
{
    /// <summary>This model is used to store, and load correlation Ids across multiple persistent sources (i.e. disk, S3, DynamoDB).</summary>
    public readonly struct CorrelationDto
    {
        /// <summary>
        /// Some globally unique Id repsenting a particular request, spanning many distributed services in order to serve it.
        /// <para>A marker to store (and later gather), logs from different systems against.</para>
        /// </summary>
        public string CorrelationId { get; init; }

        /// <summary>UTC+00:00 in ISO 8601 format.</summary>
        public string Created { get; init; }

        /// <summary>This ctor is used by the json serializer, when reading DTOs.</summary>
        public CorrelationDto() { }

        /// <summary>Use this ctor when creating new DTOs.</summary>
        public CorrelationDto(string correlationId)
        {
            CorrelationId = correlationId;
            Created = DateTimeOffset.UtcNow.ToString("O");
        }

        public override string ToString() => CorrelationId ?? string.Empty;
    }
}
