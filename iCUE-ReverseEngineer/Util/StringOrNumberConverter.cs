using System.Text.Json;
using System.Text.Json.Serialization;

namespace iCUE_ReverseEngineer.Util;

internal class StringOrNumberConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetInt32().ToString(), // or reader.GetDouble().ToString()
            _ => throw new JsonException("StringOrNumberConverter can only convert string or number types.")
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}