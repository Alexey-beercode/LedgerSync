using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LedgerSync.Application.Common.Serialization;

/// <summary>
/// Writes timestamps in UTC with the "Z" suffix (<c>2026-06-20T10:00:00Z</c>), as in the assignment contract,
/// instead of the default <c>+00:00</c> offset notation.
/// </summary>
public sealed class UtcDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    private const string Format = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'";

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetDateTimeOffset().ToUniversalTime();

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.UtcDateTime.ToString(Format, CultureInfo.InvariantCulture));
}
