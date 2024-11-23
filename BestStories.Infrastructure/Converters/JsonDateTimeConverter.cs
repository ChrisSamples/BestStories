using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BestStories.Infrastructure
{
    /// <summary>
    /// Converts date time to UTC, with time zone portion of +00:00.
    /// </summary>
    public class JsonDateTimeConverter : JsonConverter<DateTime>
    {
        private const string DATETIME_FORMAT = "yyyy-MM-ddTHH:mm:ss";
        private const string TIMEZONE_SUFFIX = "+00:00";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? dateString = reader.GetString();
            if (dateString is null)
                throw new JsonException("Expected a non-null string for DateTime.");

            return DateTime.Parse(dateString, CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // ISO 8601 with time zone suffix
            writer.WriteStringValue(value.ToUniversalTime().ToString(DATETIME_FORMAT) + TIMEZONE_SUFFIX);
        }
    }
}
