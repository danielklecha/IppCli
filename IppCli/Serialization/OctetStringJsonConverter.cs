using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharpIpp.Protocol.Models;

namespace IppCli.Serialization;

public sealed class OctetStringJsonConverter : JsonConverter<OctetString>
{
    public override OctetString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return default;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            return new OctetString(str ?? string.Empty);
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            bool? isNoValue = null;
            string? strVal = null;
            byte[]? bytesVal = null;

            foreach (var prop in root.EnumerateObject())
            {
                if (string.Equals(prop.Name, "isNoValue", StringComparison.OrdinalIgnoreCase))
                {
                    if (prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False)
                    {
                        isNoValue = prop.Value.GetBoolean();
                    }
                }
                else if (string.Equals(prop.Name, "isValue", StringComparison.OrdinalIgnoreCase))
                {
                    if (prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False)
                    {
                        isNoValue = !prop.Value.GetBoolean();
                    }
                }
                else if (string.Equals(prop.Name, "value", StringComparison.OrdinalIgnoreCase))
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        strVal = prop.Value.GetString();
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        bytesVal = JsonSerializer.Deserialize<byte[]>(prop.Value.GetRawText(), options);
                    }
                }
            }

            if (isNoValue == true)
            {
                return default;
            }

            if (bytesVal != null)
            {
                return new OctetString(bytesVal);
            }

            if (strVal != null)
            {
                return new OctetString(strVal);
            }

            if (isNoValue == false)
            {
                return new OctetString(string.Empty);
            }

            return default;
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var bytes = JsonSerializer.Deserialize<byte[]>(ref reader, options);
            return bytes != null ? new OctetString(bytes) : default;
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, OctetString value, JsonSerializerOptions options)
    {
        if (!value.IsValue)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.ToString());
    }
}
