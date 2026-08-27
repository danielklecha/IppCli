using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SharpIpp.Protocol;
using SharpIpp.Protocol.Models;

namespace IppCli.Services;

public static partial class BaseRenderer
{
    [GeneratedRegex(@"(\B[A-Z])")]
    private static partial Regex SectionTitleRegex();

    public static string FormatSectionTitle(string name)
    {
        return SectionTitleRegex().Replace(name, " $1");
    }

    public static bool IsComplexType(Type type)
    {
        var targetType = Nullable.GetUnderlyingType(type) ?? type;

        if (targetType.IsPrimitive || targetType.IsEnum)
            return false;

        if (typeof(ISmartEnum).IsAssignableFrom(targetType))
            return false;

        if (targetType == typeof(string) ||
            targetType == typeof(decimal) ||
            targetType == typeof(DateTime) ||
            targetType == typeof(DateTimeOffset) ||
            targetType == typeof(TimeSpan) ||
            targetType == typeof(Guid) ||
            targetType == typeof(Uri) ||
            targetType == typeof(IppVersion) ||
            targetType == typeof(IppStatusCode) ||
            targetType == typeof(byte[]))
        {
            return false;
        }

        return targetType.IsClass || (targetType.IsValueType && !targetType.IsPrimitive && targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Length > 0);
    }

    public static bool IsComplexObject(object? obj)
    {
        if (obj == null) return false;
        return IsComplexType(obj.GetType());
    }

    public static string FormatValue(object? value)
    {
        if (value == null)
            return "null";

        if (value is IEnumerable enumerable and not string and not byte[])
        {
            var items = enumerable.Cast<object>().Select(x => x?.ToString() ?? "null").ToList();
            if (items.Count == 0) return "[]";
            return string.Join(", ", items);
        }

        return value.ToString() ?? "null";
    }

    public static IEnumerable<PropertyInfo> GetResponseProperties(IIppResponse response)
    {
        return response.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name != nameof(IIppResponse.StatusCode)
                     && p.Name != nameof(IIppResponse.Version)
                     && p.Name != nameof(IIppResponse.RequestId)
                     && p.Name != nameof(IIppResponse.OperationAttributes));
    }

    public static IOrderedEnumerable<PropertyInfo> GetObjectProperties(object obj)
    {
        return obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(p => p.Name);
    }
}

