using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IppCli.Serialization;

public sealed class SharpIppSmartEnumJsonConverter<T> : JsonConverter<T> where T : struct
{
    private static readonly Dictionary<string, T> StaticLookups = BuildStaticLookups();
    private static readonly ConcurrentDictionary<string, T?> DynamicStringCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConstructorInfo? StringCtor = FindStringConstructor();
    private static readonly ConstructorInfo? IntCtor = FindIntConstructor();

    private static Dictionary<string, T> BuildStaticLookups()
    {
        var dict = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);

        // 1. Static fields (e.g. Sides.TwoSidedLongEdge, DocumentFormat.ApplicationPdf)
        var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static);
        foreach (var f in fields)
        {
            var valObj = f.GetValue(null);
            if (valObj is T val)
            {
                dict[f.Name] = val;
                dict[f.Name.Replace("-", "").Replace("_", "")] = val;

                var strVal = val.ToString();
                if (!string.IsNullOrWhiteSpace(strVal))
                {
                    dict[strVal] = val;
                    dict[strVal.Replace("-", "").Replace("_", "")] = val;
                }
            }
        }

        // 2. Static properties
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Static);
        foreach (var p in props)
        {
            if (p.CanRead)
            {
                var valObj = p.GetValue(null);
                if (valObj is T val)
                {
                    dict[p.Name] = val;
                    dict[p.Name.Replace("-", "").Replace("_", "")] = val;

                    var strVal = val.ToString();
                    if (!string.IsNullOrWhiteSpace(strVal))
                    {
                        dict[strVal] = val;
                        dict[strVal.Replace("-", "").Replace("_", "")] = val;
                    }
                }
            }
        }

        return dict;
    }

    private static ConstructorInfo? FindStringConstructor()
    {
        return typeof(T).GetConstructors()
            .FirstOrDefault(c =>
            {
                var parameters = c.GetParameters();
                return parameters.Length > 0 &&
                       parameters[0].ParameterType == typeof(string) &&
                       parameters.Skip(1).All(p => p.HasDefaultValue || p.IsOptional);
            });
    }

    private static ConstructorInfo? FindIntConstructor()
    {
        return typeof(T).GetConstructors()
            .FirstOrDefault(c =>
            {
                var parameters = c.GetParameters();
                return parameters.Length > 0 &&
                       (parameters[0].ParameterType == typeof(int) || parameters[0].ParameterType == typeof(short)) &&
                       parameters.Skip(1).All(p => p.HasDefaultValue || p.IsOptional);
            });
    }

    private static object?[] BuildArgs(ConstructorInfo ctor, object firstArg)
    {
        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];
        args[0] = firstArg;
        for (int i = 1; i < parameters.Length; i++)
        {
            args[i] = parameters[i].HasDefaultValue
                ? parameters[i].DefaultValue
                : (parameters[i].ParameterType.IsValueType ? Activator.CreateInstance(parameters[i].ParameterType) : null);
        }
        return args;
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (string.IsNullOrWhiteSpace(str))
                return default;

            return ResolveFromString(str!);
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out var intVal))
            {
                if (IntCtor != null)
                {
                    try
                    {
                        var args = BuildArgs(IntCtor, intVal);
                        return (T)IntCtor.Invoke(args);
                    }
                    catch
                    {
                        // ignore
                    }
                }
                return ResolveFromString(intVal.ToString());
            }
        }

        return default;
    }

    private static T ResolveFromString(string rawStr)
    {
        if (StaticLookups.TryGetValue(rawStr, out var staticVal))
        {
            return staticVal;
        }

        var normalized = rawStr.Replace("-", "").Replace("_", "");
        if (StaticLookups.TryGetValue(normalized, out var normVal))
        {
            return normVal;
        }

        if (DynamicStringCache.TryGetValue(rawStr, out var cached) && cached.HasValue)
        {
            return cached.Value;
        }

        // Try string constructor for custom values (e.g. custom MIME types or action strings)
        if (StringCtor != null)
        {
            try
            {
                var args = BuildArgs(StringCtor, rawStr);
                var created = (T)StringCtor.Invoke(args);
                DynamicStringCache[rawStr] = created;
                return created;
            }
            catch
            {
                // ignore
            }
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
