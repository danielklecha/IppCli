using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharpIpp.Protocol.Models;

namespace IppCli.Serialization;

public sealed class StringWithLanguageJsonConverter : JsonConverter<StringWithLanguage>
{
    public override StringWithLanguage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return default;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            return new StringWithLanguage(str ?? string.Empty);
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            bool? isNoValue = null;
            string? lang = null;
            string? val = null;

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
                else if (string.Equals(prop.Name, "language", StringComparison.OrdinalIgnoreCase))
                {
                    lang = prop.Value.ValueKind == JsonValueKind.Null ? null : prop.Value.GetString();
                }
                else if (string.Equals(prop.Name, "value", StringComparison.OrdinalIgnoreCase))
                {
                    val = prop.Value.ValueKind == JsonValueKind.Null ? null : prop.Value.GetString();
                }
            }

            if (isNoValue == true)
            {
                return default;
            }

            if (lang != null || val != null)
            {
                return new StringWithLanguage(lang, val ?? string.Empty);
            }

            if (isNoValue == false)
            {
                return new StringWithLanguage(string.Empty);
            }

            return default;
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, StringWithLanguage value, JsonSerializerOptions options)
    {
        if (!value.IsValue)
        {
            writer.WriteNullValue();
            return;
        }

        if (value.HasLanguage)
        {
            writer.WriteStartObject();
            writer.WriteString("language", value.Language);
            writer.WriteString("value", value.Value);
            writer.WriteEndObject();
        }
        else
        {
            writer.WriteStringValue(value.Value);
        }
    }
}
