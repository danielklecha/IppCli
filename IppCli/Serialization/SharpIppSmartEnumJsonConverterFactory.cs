using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharpIpp.Protocol.Models;

namespace IppCli.Serialization;

public sealed class SharpIppSmartEnumJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(ISmartEnum).IsAssignableFrom(typeToConvert);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(SharpIppSmartEnumJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
