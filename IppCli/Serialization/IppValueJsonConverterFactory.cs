using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharpIpp.Protocol.Models;

namespace IppCli.Serialization;

public sealed class IppValueJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(IppValue<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(IppValueJsonConverter<>).MakeGenericType(valueType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

public sealed class IppValueJsonConverter<T> : JsonConverter<IppValue<T>>
{
    public override IppValue<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return IppValue<T>.NoValue;
        }

        if (reader.TokenType == JsonTokenType.String && string.Equals(reader.GetString(), "no-value", StringComparison.OrdinalIgnoreCase))
        {
            return IppValue<T>.NoValue;
        }

        var value = JsonSerializer.Deserialize<T>(ref reader, options);
        return new IppValue<T>(value);
    }

    public override void Write(Utf8JsonWriter writer, IppValue<T> value, JsonSerializerOptions options)
    {
        if (!value.IsValue)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, value.Value, options);
    }
}
