using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IppCli.Serialization;

public sealed class RangeJsonConverter : JsonConverter<SharpIpp.Protocol.Models.Range>
{
    public override SharpIpp.Protocol.Models.Range Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (!string.IsNullOrWhiteSpace(str))
            {
                var parts = str!.Split('-');
                if (parts.Length > 1 && int.TryParse(parts[0].Trim(), out var lower) && int.TryParse(parts[1].Trim(), out var upper))
                {
                    return new SharpIpp.Protocol.Models.Range(lower, upper);
                }
                if (int.TryParse(parts[0].Trim(), out var single))
                {
                    return new SharpIpp.Protocol.Models.Range(single, single);
                }
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            var val = reader.GetInt32();
            return new SharpIpp.Protocol.Models.Range(val, val);
        }
        else if (reader.TokenType == JsonTokenType.StartArray)
        {
            int? first = null;
            int? second = null;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var val))
                {
                    if (!first.HasValue)
                    {
                        first = val;
                    }
                    else if (!second.HasValue)
                    {
                        second = val;
                    }
                }
            }

            if (first.HasValue && second.HasValue)
            {
                return new SharpIpp.Protocol.Models.Range(first.Value, second.Value);
            }
            if (first.HasValue)
            {
                return new SharpIpp.Protocol.Models.Range(first.Value, first.Value);
            }
        }
        else if (reader.TokenType == JsonTokenType.StartObject)
        {
            int? lower = null;
            int? upper = null;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propName = reader.GetString();
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var val))
                    {
                        if (string.Equals(propName, "lower", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(propName, "from", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(propName, "min", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(propName, "start", StringComparison.OrdinalIgnoreCase))
                        {
                            lower = val;
                        }
                        else if (string.Equals(propName, "upper", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(propName, "to", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(propName, "max", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(propName, "end", StringComparison.OrdinalIgnoreCase))
                        {
                            upper = val;
                        }
                    }
                }
            }

            if (lower.HasValue && upper.HasValue)
            {
                return new SharpIpp.Protocol.Models.Range(lower.Value, upper.Value);
            }
            if (lower.HasValue)
            {
                return new SharpIpp.Protocol.Models.Range(lower.Value, lower.Value);
            }
            if (upper.HasValue)
            {
                return new SharpIpp.Protocol.Models.Range(upper.Value, upper.Value);
            }
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, SharpIpp.Protocol.Models.Range value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
