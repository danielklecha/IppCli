using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharpIpp.Protocol.Models;

namespace IppCli.Serialization;

public sealed class IppVersionJsonConverter : JsonConverter<IppVersion>
{
    public override IppVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (!string.IsNullOrWhiteSpace(str))
            {
                return new IppVersion(str!);
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            var num = reader.GetDouble();
            return new IppVersion(num.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
        }

        return new IppVersion(1, 1);
    }

    public override void Write(Utf8JsonWriter writer, IppVersion value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
