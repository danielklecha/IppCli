using System;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace IppCli.Serialization;

public static class IppJsonHelper
{
    private static readonly Lazy<JsonSerializerOptions> LazyDefaultOptions = new(CreateDefaultOptions);
    private static readonly Lazy<JsonSerializerOptions> LazyOutputOptions = new(CreateOutputOptions);
    private static readonly Lazy<JsonSerializerOptions> LazyCompactOutputOptions = new(CreateCompactOutputOptions);

    public static JsonSerializerOptions DefaultOptions => LazyDefaultOptions.Value;
    public static JsonSerializerOptions OutputOptions => LazyOutputOptions.Value;
    public static JsonSerializerOptions CompactOutputOptions => LazyCompactOutputOptions.Value;

    private static void AddIppConverters(JsonSerializerOptions options)
    {
        options.Converters.Add(new IppVersionJsonConverter());
        options.Converters.Add(new RangeJsonConverter());
        options.Converters.Add(new SharpIppSmartEnumJsonConverterFactory());
    }

    private static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true));
        AddIppConverters(options);
        return options;
    }

    private static JsonSerializerOptions CreateOutputOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.Converters.Add(new JsonStringEnumConverter());
        AddIppConverters(options);
        return options;
    }

    private static JsonSerializerOptions CreateCompactOutputOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.Converters.Add(new JsonStringEnumConverter());
        AddIppConverters(options);
        return options;
    }

    public static T? Deserialize<T>(string value, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return default;

        var json = ResolveJsonContent(value);
        return JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
    }

    public static string Serialize<T>(T value, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(value, options ?? DefaultOptions);
    }

    public static string Serialize(object value, Type inputType, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(value, inputType, options ?? DefaultOptions);
    }

    public static string SerializeOutput(object value)
    {
        return JsonSerializer.Serialize(value, value.GetType(), OutputOptions);
    }

    public static string SerializeCompact(object value)
    {
        return JsonSerializer.Serialize(value, value.GetType(), CompactOutputOptions);
    }

    private static string ResolveJsonContent(string value)
    {
        var trimmed = value.Trim();
        string rawJson;
        if (trimmed.StartsWith("@", StringComparison.Ordinal))
        {
            var rawPath = trimmed.Substring(1).Trim('"', '\'');
            var filePath = ExpandPath(rawPath);
            var fullPath = Path.GetFullPath(filePath);

            if (File.Exists(fullPath))
            {
                rawJson = File.ReadAllText(fullPath);
            }
            else
            {
                throw new FileNotFoundException($"JSON attributes file not found: '{rawPath}' (resolved to '{fullPath}')", fullPath);
            }
        }
        else
        {
            rawJson = trimmed;
        }

        return NormalizeJson(rawJson);
    }

    public static string ExpandPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        if (path == "~" || path.StartsWith("~/", StringComparison.Ordinal) || path.StartsWith(@"~\", StringComparison.Ordinal))
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(userProfile))
            {
                return Path.Combine(userProfile, path.Length > 2 ? path.Substring(2) : string.Empty);
            }
        }

        return Environment.ExpandEnvironmentVariables(path);
    }

    private static string NormalizeJson(string json)
    {
        try
        {
            var node = JsonNode.Parse(json);
            if (node != null)
            {
                NormalizeNode(node);
                return node.ToJsonString();
            }
        }
        catch
        {
            // If parsing fails, fall back to raw input and let JsonSerializer report the error
        }
        return json;
    }

    private static void NormalizeNode(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            var properties = obj.ToList();
            foreach (var kvp in properties)
            {
                var key = kvp.Key;
                var val = kvp.Value;
                if (val != null)
                {
                    NormalizeNode(val);
                }

                if (key.Contains('-') || key.Contains('_'))
                {
                    var normalizedKey = ToCamelCase(key);
                    if (normalizedKey != key && !obj.ContainsKey(normalizedKey))
                    {
                        obj.Remove(key);
                        obj[normalizedKey] = val;
                    }
                }
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item != null)
                {
                    NormalizeNode(item);
                }
            }
        }
    }

    private static string ToCamelCase(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        var parts = key.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return key;
        if (parts.Length == 1)
        {
            return parts[0].Length > 0 && char.IsUpper(parts[0][0])
                ? char.ToLowerInvariant(parts[0][0]) + parts[0].Substring(1)
                : parts[0];
        }

        var sb = new System.Text.StringBuilder(key.Length);
        sb.Append(char.ToLowerInvariant(parts[0][0]));
        if (parts[0].Length > 1)
        {
            sb.Append(parts[0].Substring(1));
        }

        for (int i = 1; i < parts.Length; i++)
        {
            var p = parts[i];
            if (p.Length > 0)
            {
                sb.Append(char.ToUpperInvariant(p[0]));
                if (p.Length > 1)
                {
                    sb.Append(p.Substring(1));
                }
            }
        }
        return sb.ToString();
    }
}
