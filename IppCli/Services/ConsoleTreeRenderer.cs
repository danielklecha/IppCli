using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SharpIpp.Protocol;
using SharpIpp.Protocol.Models;
using Spectre.Console;

namespace IppCli.Services;

public class ConsoleTreeRenderer : IOutputRenderer
{
    private static readonly ConsoleTreeRenderer DefaultInstance = new();
    private readonly IAnsiConsole _console;

    private static readonly HashSet<Type> DefaultLeafTypes =
    [
        typeof(string),
        typeof(decimal),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid),
        typeof(Uri),
        typeof(byte[]),
        typeof(IppVersion),
        typeof(IppStatusCode),
        typeof(SharpIpp.Protocol.Models.Range),
        typeof(Resolution),
        typeof(ISmartEnum),
        typeof(IIppStructuredString)
    ];

    public ConsoleTreeRenderer(IAnsiConsole? console = null)
    {
        _console = console ?? AnsiConsole.Console;
    }

    public static void Render(string operationName, IIppResponse response)
    {
        DefaultInstance.RenderResponse(operationName, response);
    }

    public static void Render(string operationName, IIppResponse response, IAnsiConsole console)
    {
        var renderer = new ConsoleTreeRenderer(console);
        renderer.RenderResponse(operationName, response);
    }

    public static void Render(string operationName, IIppResponse response, TextWriter writer)
    {
        DefaultInstance.RenderResponse(operationName, response, writer);
    }

    public static void Render<T>(
        T? obj,
        string? rootTitle = null,
        IEnumerable<Type>? leafTypes = null,
        IAnsiConsole? console = null)
    {
        var targetConsole = console ?? AnsiConsole.Console;
        var tree = CreateTree(obj, rootTitle, leafTypes);
        targetConsole.Write(tree);
    }

    public void RenderResponse(string operationName, IIppResponse response, TextWriter? writer = null)
    {
        var tree = CreateTree(response, operationName, leafTypes: new[] { typeof(IppVersion) });

        if (writer != null && writer != Console.Out)
        {
            var fileConsole = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.No,
                ColorSystem = ColorSystemSupport.NoColors,
                Out = new AnsiConsoleOutput(writer)
            });
            fileConsole.Profile.Width = 200;
            fileConsole.Write(tree);
            return;
        }

        _console.Write(tree);
    }

    /// <summary>
    /// Generic method that accepts any object, iterates through properties, and builds a Spectre.Console Tree with "key: value".
    /// </summary>
    /// <typeparam name="T">The type of object to inspect.</typeparam>
    /// <param name="obj">The object to render.</param>
    /// <param name="rootTitle">Optional root node title.</param>
    /// <param name="leafTypes">Optional list of types that will be displayed using ToString() rather than expanded into subproperties.</param>
    /// <returns>A Spectre.Console Tree representation of the object.</returns>
    public static Tree CreateTree<T>(
        T? obj,
        string? rootTitle = null,
        IEnumerable<Type>? leafTypes = null)
    {
        var title = !string.IsNullOrWhiteSpace(rootTitle)
            ? rootTitle
            : (obj != null ? obj.GetType().Name : typeof(T).Name);

        var tree = new Tree($"[bold cyan]{Markup.Escape(title)}[/]");

        if (obj == null)
        {
            tree.AddNode("[grey]null[/]");
            return tree;
        }

        var customLeafTypesSet = leafTypes != null ? new HashSet<Type>(leafTypes) : null;
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);

        // If the root object itself is a leaf type
        if (IsLeaf(obj, customLeafTypesSet))
        {
            tree.AddNode(FormatLeafValue(obj));
            return tree;
        }

        // If the root object is an IEnumerable (and not string/byte[])
        if (obj is IEnumerable enumerable and not string and not byte[])
        {
            PopulateEnumerableNodes(tree, enumerable, customLeafTypesSet, visited, 0);
            return tree;
        }

        // Otherwise populate object properties
        visited.Add(obj);
        PopulateObjectProperties(tree, obj, customLeafTypesSet, visited, 0);

        return tree;
    }

    public static bool IsLeaf(object? value, HashSet<Type>? customLeafTypes = null)
    {
        if (value == null)
        {
            return true;
        }

        if (value is INoValue { IsValue: false })
        {
            return true;
        }

        var type = value is Type t ? t : value.GetType();
        var targetType = Nullable.GetUnderlyingType(type) ?? type;

        if (customLeafTypes != null && customLeafTypes.Any(t => t.IsAssignableFrom(targetType) || t == targetType))
        {
            return true;
        }

        if (targetType.IsPrimitive || targetType.IsEnum)
        {
            return true;
        }

        if (DefaultLeafTypes.Contains(targetType) || DefaultLeafTypes.Any(t => t.IsAssignableFrom(targetType)))
        {
            return true;
        }

        return false;
    }

    private static string FormatLeafValue(object? value)
    {
        if (value == null) return "[grey]null[/]";

        if (value is INoValue { IsValue: false })
        {
            return "[grey]no value[/]";
        }

        if (value is IppStatusCode statusCode)
        {
            var statusColor = GetStatusColor(statusCode);
            return $"[{statusColor}]{statusCode} (0x{(short)statusCode:X4})[/]";
        }

        if (value is bool b)
        {
            return b ? "[green]true[/]" : "[red]false[/]";
        }

        if (value is byte[] bytes)
        {
            return $"[dim]{bytes.Length} bytes[/]";
        }

        return Markup.Escape(value.ToString() ?? "null");
    }

    private static string GetStatusColor(IppStatusCode statusCode)
    {
        var code = (short)statusCode;
        if (code >= 0x0000 && code <= 0x00FF) return "green";
        if (code >= 0x0100 && code <= 0x01FF) return "yellow";
        if (code >= 0x0400 && code <= 0x04FF) return "red";
        if (code >= 0x0500 && code <= 0x05FF) return "red bold";
        return "white";
    }

    private static void PopulateObjectProperties(
        IHasTreeNodes parentNode,
        object obj,
        HashSet<Type>? customLeafTypes,
        HashSet<object> visited,
        int depth)
    {
        if (depth > 20)
        {
            parentNode.AddNode("[dim]... (max depth reached)[/]");
            return;
        }

        var properties = obj.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .OrderBy(p => p.Name);

        foreach (var prop in properties)
        {
            var val = prop.GetValue(obj);
            if (val == null) continue; // Skip null properties

            AddPropertyNode(parentNode, prop.Name, val, customLeafTypes, visited, depth + 1);
        }
    }

    private static void AddPropertyNode(
        IHasTreeNodes parentNode,
        string key,
        object value,
        HashSet<Type>? customLeafTypes,
        HashSet<object> visited,
        int depth)
    {
        // 1. Leaf type (including INoValue with IsValue == false)
        if (IsLeaf(value, customLeafTypes))
        {
            parentNode.AddNode($"[bold]{Markup.Escape(key)}:[/] {FormatLeafValue(value)}");
            return;
        }

        // 2. Dictionary
        if (value is IDictionary dict)
        {
            var dictNode = parentNode.AddNode($"[bold yellow]{Markup.Escape(key)}[/] [dim]({dict.Count})[/]");
            foreach (DictionaryEntry entry in dict)
            {
                var entryKey = entry.Key?.ToString() ?? "null";
                if (entry.Value == null)
                {
                    dictNode.AddNode($"[bold]{Markup.Escape(entryKey)}:[/] [grey]null[/]");
                }
                else
                {
                    AddPropertyNode(dictNode, entryKey, entry.Value, customLeafTypes, visited, depth + 1);
                }
            }
            return;
        }

        // 3. Generic IEnumerable / Array / List (and not string / not byte[])
        if (value is IEnumerable enumerable and not string and not byte[])
        {
            var items = enumerable.Cast<object?>().Where(x => x != null).ToList();
            if (items.Count == 0)
            {
                parentNode.AddNode($"[bold]{Markup.Escape(key)}:[/] [dim][[]][/]");
                return;
            }

            var allLeaves = items.All(x => IsLeaf(x, customLeafTypes));
            if (allLeaves)
            {
                var formattedItems = items.Select(FormatLeafValue);
                parentNode.AddNode($"[bold]{Markup.Escape(key)}[/] [dim]({items.Count}):[/] {string.Join("[grey], [/]", formattedItems)}");
                return;
            }

            var listNode = parentNode.AddNode($"[bold yellow]{Markup.Escape(key)}[/] [dim]({items.Count})[/]");
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i]!;
                if (IsLeaf(item, customLeafTypes))
                {
                    listNode.AddNode($"[dim][[{i}]][/] {FormatLeafValue(item)}");
                }
                else
                {
                    if (!visited.Add(item))
                    {
                        listNode.AddNode($"[dim][[{i}]][/] [dim](circular reference)[/]");
                        continue;
                    }

                    var itemNode = listNode.AddNode($"[dim][[{i}]][/]");
                    PopulateObjectProperties(itemNode, item, customLeafTypes, visited, depth + 1);
                }
            }
            return;
        }

        // 4. Complex nested object
        if (!visited.Add(value))
        {
            parentNode.AddNode($"[bold]{Markup.Escape(key)}:[/] [dim](circular reference)[/]");
            return;
        }

        var objNode = parentNode.AddNode($"[bold yellow]{Markup.Escape(key)}[/]");
        PopulateObjectProperties(objNode, value, customLeafTypes, visited, depth + 1);
    }

    private static void PopulateEnumerableNodes(
        IHasTreeNodes parentNode,
        IEnumerable enumerable,
        HashSet<Type>? customLeafTypes,
        HashSet<object> visited,
        int depth)
    {
        var items = enumerable.Cast<object?>().Where(x => x != null).ToList();
        if (items.Count == 0)
        {
            parentNode.AddNode("[dim]Empty collection[/]");
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i]!;
            if (IsLeaf(item, customLeafTypes))
            {
                parentNode.AddNode($"[dim][[{i}]][/] {FormatLeafValue(item)}");
            }
            else
            {
                if (!visited.Add(item))
                {
                    parentNode.AddNode($"[dim][[{i}]][/] [dim](circular reference)[/]");
                    continue;
                }

                var itemNode = parentNode.AddNode($"[dim][[{i}]][/]");
                PopulateObjectProperties(itemNode, item, customLeafTypes, visited, depth + 1);
            }
        }
    }
}
