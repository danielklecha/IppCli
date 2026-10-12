using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace IppCli.Generators;

/// <summary>
/// Roslyn incremental source generator that analyzes all types in the SharpIpp namespace
/// and generates static, reflection-free Spectre.Console tree renderers.
/// </summary>
[Generator]
public class ConsoleTreeRendererSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var compilationProvider = context.CompilationProvider;
        context.RegisterSourceOutput(compilationProvider, static (spc, compilation) =>
        {
            Execute(compilation, spc);
        });
    }

    private static void Execute(Compilation compilation, SourceProductionContext context)
    {
        // 1. Ensure SharpIpp is available in compilation references
        var ippResponseType = compilation.GetTypeByMetadataName("SharpIpp.Protocol.IIppResponse");
        if (ippResponseType == null)
        {
            return;
        }

        // 2. Prevent duplicate generation in referencing assemblies (e.g. IppCli.Tests referencing IppCli)
        var existingRenderer = compilation.GetTypeByMetadataName("IppCli.Services.ConsoleTreeRenderer");
        if (existingRenderer != null && !SymbolEqualityComparer.Default.Equals(existingRenderer.ContainingAssembly, compilation.Assembly))
        {
            return;
        }

        // 3. Collect and filter all types in SharpIpp namespace
        var allSharpIppTypes = GetAllSharpIppTypes(compilation);
        var candidateTypes = allSharpIppTypes
            .Where(t => IsRenderableComplexType(t))
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
            .OrderByDescending(GetInheritanceDepth)
            .ThenBy(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal)
            .ToList();

        // 4. Generate the source code for ConsoleTreeRenderer
        var sourceCode = GenerateRendererSource(candidateTypes);
        context.AddSource("ConsoleTreeRenderer.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
    }

    private static List<INamedTypeSymbol> GetAllSharpIppTypes(Compilation compilation)
    {
        var list = new List<INamedTypeSymbol>();
        var seenTypes = new HashSet<string>(StringComparer.Ordinal);

        void WalkNamespace(INamespaceSymbol ns)
        {
            var nsName = ns.ToDisplayString();
            bool isSharpIpp = nsName == "SharpIpp" || nsName.StartsWith("SharpIpp.", StringComparison.Ordinal);

            if (isSharpIpp)
            {
                foreach (var type in ns.GetTypeMembers())
                {
                    WalkType(type);
                }
            }

            foreach (var subNs in ns.GetNamespaceMembers())
            {
                var subNsName = subNs.ToDisplayString();
                if (subNsName == "SharpIpp" || subNsName.StartsWith("SharpIpp.", StringComparison.Ordinal) || ns.IsGlobalNamespace)
                {
                    WalkNamespace(subNs);
                }
            }
        }

        void WalkType(INamedTypeSymbol type)
        {
            var id = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (seenTypes.Add(id))
            {
                list.Add(type);
                foreach (var nested in type.GetTypeMembers())
                {
                    WalkType(nested);
                }
            }
        }

        // Traverse merged global namespace
        WalkNamespace(compilation.GlobalNamespace);

        // Also explicitly traverse referenced SharpIpp assemblies
        foreach (var asm in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (asm.Name.StartsWith("SharpIpp", StringComparison.OrdinalIgnoreCase))
            {
                WalkNamespace(asm.GlobalNamespace);
            }
        }

        return list;
    }

    private static bool IsRenderableComplexType(INamedTypeSymbol type)
    {
        if (type.DeclaredAccessibility != Accessibility.Public) return false;
        if (type.IsStatic) return false;
        if (type.IsAbstract) return false;
        if (type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct) return false;
        if (type.IsGenericType) return false;
        if (IsSubclassOf(type, "System.Attribute")) return false;
        if (IsSubclassOf(type, "System.Exception")) return false;
        if (IsLeafType(type)) return false;

        return true;
    }

    private static bool IsSubclassOf(INamedTypeSymbol? type, string baseTypeFullName)
    {
        var current = type?.BaseType;
        while (current != null && current.SpecialType != SpecialType.System_Object)
        {
            if (current.ToDisplayString() == baseTypeFullName) return true;
            current = current.BaseType;
        }
        return false;
    }

    private static bool IsLeafType(INamedTypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Enum) return true;
        if (type.SpecialType != SpecialType.None && type.SpecialType != SpecialType.System_Object) return true;

        var fullName = type.ToDisplayString();
        switch (fullName)
        {
            case "System.DateTime":
            case "System.DateTimeOffset":
            case "System.TimeSpan":
            case "System.Guid":
            case "System.Uri":
            case "System.Decimal":
            case "SharpIpp.Protocol.Models.IppVersion":
            case "SharpIpp.Protocol.Models.IppStatusCode":
            case "SharpIpp.Protocol.Models.Range":
            case "SharpIpp.Protocol.Models.Resolution":
            case "SharpIpp.Protocol.Models.StringWithLanguage":
            case "SharpIpp.Protocol.Models.OctetString":
            case "SharpIpp.Protocol.Models.IppAttribute":
                return true;
        }

        foreach (var iface in type.AllInterfaces)
        {
            var ifaceName = iface.ToDisplayString();
            if (ifaceName == "SharpIpp.Protocol.Models.IKeywordEnum" ||
                ifaceName == "SharpIpp.Protocol.Models.IIppStructuredString" ||
                ifaceName == "SharpIpp.Protocol.Models.INoValue")
            {
                return true;
            }
        }

        return false;
    }

    private static int GetInheritanceDepth(ITypeSymbol type)
    {
        int depth = 0;
        var current = type.BaseType;
        while (current != null && current.SpecialType != SpecialType.System_Object)
        {
            depth++;
            current = current.BaseType;
        }
        return depth;
    }

    private static List<IPropertySymbol> GetRenderableProperties(INamedTypeSymbol type)
    {
        var list = new List<IPropertySymbol>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var current = type;

        while (current != null && current.SpecialType != SpecialType.System_Object)
        {
            foreach (var member in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (member.DeclaredAccessibility != Accessibility.Public ||
                    member.IsStatic ||
                    member.IsIndexer ||
                    member.Parameters.Length > 0 ||
                    member.GetMethod == null ||
                    member.GetMethod.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                if (!IsTypeAccessible(member.Type))
                {
                    continue;
                }

                if (seen.Add(member.Name))
                {
                    list.Add(member);
                }
            }
            current = current.BaseType;
        }

        return list.OrderBy(p => p.Name, StringComparer.Ordinal).ToList();
    }

    private static bool IsTypeAccessible(ITypeSymbol type)
    {
        if (type.DeclaredAccessibility != Accessibility.Public &&
            type.DeclaredAccessibility != Accessibility.NotApplicable)
        {
            return false;
        }

        if (type is INamedTypeSymbol named && named.IsGenericType)
        {
            foreach (var arg in named.TypeArguments)
            {
                if (!IsTypeAccessible(arg)) return false;
            }
        }

        return true;
    }

    private static bool IsNullableValueType(ITypeSymbol type)
    {
        return type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
    }

    private static string GenerateRendererSource(List<INamedTypeSymbol> candidateTypes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("#pragma warning disable CS0618, CS0612, CS8600, CS8601, CS8602, CS8603, CS8604, CS8618");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.IO;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using SharpIpp.Protocol;");
        sb.AppendLine("using SharpIpp.Protocol.Models;");
        sb.AppendLine("using Spectre.Console;");
        sb.AppendLine();
        sb.AppendLine("namespace IppCli.Services;");
        sb.AppendLine();
        sb.AppendLine("public partial class ConsoleTreeRenderer : IOutputRenderer");
        sb.AppendLine("{");
        sb.AppendLine("    private static readonly ConsoleTreeRenderer DefaultInstance = new();");
        sb.AppendLine("    private readonly IAnsiConsole _console;");
        sb.AppendLine();
        sb.AppendLine("    public ConsoleTreeRenderer(IAnsiConsole? console = null)");
        sb.AppendLine("    {");
        sb.AppendLine("        _console = console ?? AnsiConsole.Console;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static void Render(string operationName, IIppResponse response)");
        sb.AppendLine("    {");
        sb.AppendLine("        DefaultInstance.RenderResponse(operationName, response);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static void Render(string operationName, IIppResponse response, IAnsiConsole console)");
        sb.AppendLine("    {");
        sb.AppendLine("        new ConsoleTreeRenderer(console).RenderResponse(operationName, response);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static void Render(string operationName, IIppResponse response, TextWriter writer)");
        sb.AppendLine("    {");
        sb.AppendLine("        DefaultInstance.RenderResponse(operationName, response, writer);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static void Render<T>(T? obj, string? rootTitle = null, IAnsiConsole? console = null)");
        sb.AppendLine("    {");
        sb.AppendLine("        var targetConsole = console ?? AnsiConsole.Console;");
        sb.AppendLine("        var tree = CreateTree(obj, rootTitle);");
        sb.AppendLine("        targetConsole.Write(tree);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static void Render<T>(T? obj, string? rootTitle, IEnumerable<Type>? leafTypes, IAnsiConsole? console = null)");
        sb.AppendLine("    {");
        sb.AppendLine("        var targetConsole = console ?? AnsiConsole.Console;");
        sb.AppendLine("        var tree = CreateTree(obj, rootTitle, leafTypes);");
        sb.AppendLine("        targetConsole.Write(tree);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public void RenderResponse(string operationName, IIppResponse response, TextWriter? writer = null)");
        sb.AppendLine("    {");
        sb.AppendLine("        var tree = CreateTree(response, operationName);");
        sb.AppendLine("        if (writer != null && writer != Console.Out)");
        sb.AppendLine("        {");
        sb.AppendLine("            var fileConsole = AnsiConsole.Create(new AnsiConsoleSettings");
        sb.AppendLine("            {");
        sb.AppendLine("                Ansi = AnsiSupport.No,");
        sb.AppendLine("                ColorSystem = ColorSystemSupport.NoColors,");
        sb.AppendLine("                Out = new AnsiConsoleOutput(writer),");
        sb.AppendLine("                Interactive = InteractionSupport.No");
        sb.AppendLine("            });");
        sb.AppendLine("            fileConsole.Profile.Capabilities.Ansi = false;");
        sb.AppendLine("            fileConsole.Profile.Capabilities.ColorSystem = ColorSystem.NoColors;");
        sb.AppendLine("            fileConsole.Profile.Width = 200;");
        sb.AppendLine("            fileConsole.Write(tree);");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        _console.Write(tree);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static Tree CreateTree(string operationName, IIppResponse response)");
        sb.AppendLine("    {");
        sb.AppendLine("        return CreateTree(response, operationName);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static Tree CreateTree<T>(T? obj, string? rootTitle = null, IEnumerable<Type>? leafTypes = null)");
        sb.AppendLine("    {");
        sb.AppendLine("        var title = !string.IsNullOrWhiteSpace(rootTitle)");
        sb.AppendLine("            ? rootTitle");
        sb.AppendLine("            : (obj != null ? obj.GetType().Name : typeof(T).Name);");
        sb.AppendLine();
        sb.AppendLine("        var tree = new Tree($\"[bold cyan]{Markup.Escape(title)}[/]\");");
        sb.AppendLine();
        sb.AppendLine("        if (obj == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            tree.AddNode(\"[grey]null[/]\");");
        sb.AppendLine("            return tree;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (obj is IIppValue rootIppVal)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!rootIppVal.IsValue)");
        sb.AppendLine("            {");
        sb.AppendLine("                tree.AddNode(\"[grey]no value[/]\");");
        sb.AppendLine("                return tree;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            var inner = rootIppVal.ValueAsObject;");
        sb.AppendLine("            if (inner == null)");
        sb.AppendLine("            {");
        sb.AppendLine("                tree.AddNode(\"[grey]null[/]\");");
        sb.AppendLine("                return tree;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            return CreateTree(inner, rootTitle, leafTypes);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        var customLeafTypesSet = leafTypes != null ? new HashSet<Type>(leafTypes) : null;");
        sb.AppendLine("        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);");
        sb.AppendLine();
        sb.AppendLine("        if (IsLeaf(obj, customLeafTypesSet))");
        sb.AppendLine("        {");
        sb.AppendLine("            tree.AddNode(FormatLeafValue(obj));");
        sb.AppendLine("            return tree;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (obj is IEnumerable enumerable and not string and not byte[])");
        sb.AppendLine("        {");
        sb.AppendLine("            RenderEnumerableNodes(tree, enumerable, visited, 0, customLeafTypesSet);");
        sb.AppendLine("            return tree;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        visited.Add(obj);");
        sb.AppendLine("        RenderObjectProperties(tree, obj, visited, 0, customLeafTypesSet);");
        sb.AppendLine("        return tree;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static bool IsLeaf(object? value, HashSet<Type>? customLeafTypes = null)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (value == null) return true;");
        sb.AppendLine();
        sb.AppendLine("        if (value is INoValue { IsValue: false }) return true;");
        sb.AppendLine();
        sb.AppendLine("        if (value is IIppValue ippVal)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!ippVal.IsValue) return true;");
        sb.AppendLine("            return IsLeaf(ippVal.ValueAsObject, customLeafTypes);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (customLeafTypes != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            var targetType = value is Type t ? t : value.GetType();");
        sb.AppendLine("            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;");
        sb.AppendLine("            while (underlying.IsGenericType && underlying.GetGenericTypeDefinition() == typeof(IppValue<>))");
        sb.AppendLine("            {");
        sb.AppendLine("                underlying = underlying.GetGenericArguments()[0];");
        sb.AppendLine("                underlying = Nullable.GetUnderlyingType(underlying) ?? underlying;");
        sb.AppendLine("            }");
        sb.AppendLine("            if (customLeafTypes.Contains(underlying) || customLeafTypes.Any(x => x.IsAssignableFrom(underlying)))");
        sb.AppendLine("                return true;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (value is Type typeVal)");
        sb.AppendLine("        {");
        sb.AppendLine("            var t = Nullable.GetUnderlyingType(typeVal) ?? typeVal;");
        sb.AppendLine("            while (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IppValue<>))");
        sb.AppendLine("            {");
        sb.AppendLine("                t = t.GetGenericArguments()[0];");
        sb.AppendLine("                t = Nullable.GetUnderlyingType(t) ?? t;");
        sb.AppendLine("            }");
        sb.AppendLine("            if (t.IsPrimitive || t.IsEnum) return true;");
        sb.AppendLine("            if (typeof(IKeywordEnum).IsAssignableFrom(t) || typeof(IIppStructuredString).IsAssignableFrom(t)) return true;");
        sb.AppendLine("            if (t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime) || t == typeof(DateTimeOffset) ||");
        sb.AppendLine("                t == typeof(TimeSpan) || t == typeof(Guid) || t == typeof(Uri) || t == typeof(byte[]) ||");
        sb.AppendLine("                t == typeof(IppVersion) || t == typeof(IppStatusCode) || t == typeof(SharpIpp.Protocol.Models.Range) ||");
        sb.AppendLine("                t == typeof(Resolution) || t == typeof(StringWithLanguage) || t == typeof(OctetString) || t == typeof(IppAttribute)) return true;");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        switch (value)");
        sb.AppendLine("        {");
        sb.AppendLine("            case string:");
        sb.AppendLine("            case bool:");
        sb.AppendLine("            case byte:");
        sb.AppendLine("            case sbyte:");
        sb.AppendLine("            case short:");
        sb.AppendLine("            case ushort:");
        sb.AppendLine("            case int:");
        sb.AppendLine("            case uint:");
        sb.AppendLine("            case long:");
        sb.AppendLine("            case ulong:");
        sb.AppendLine("            case float:");
        sb.AppendLine("            case double:");
        sb.AppendLine("            case decimal:");
        sb.AppendLine("            case char:");
        sb.AppendLine("            case DateTime:");
        sb.AppendLine("            case DateTimeOffset:");
        sb.AppendLine("            case TimeSpan:");
        sb.AppendLine("            case Guid:");
        sb.AppendLine("            case Uri:");
        sb.AppendLine("            case byte[]:");
        sb.AppendLine("            case IppVersion:");
        sb.AppendLine("            case IppStatusCode:");
        sb.AppendLine("            case SharpIpp.Protocol.Models.Range:");
        sb.AppendLine("            case Resolution:");
        sb.AppendLine("            case IKeywordEnum:");
        sb.AppendLine("            case IIppStructuredString:");
        sb.AppendLine("            case StringWithLanguage:");
        sb.AppendLine("            case OctetString:");
        sb.AppendLine("            case IppAttribute:");
        sb.AppendLine("            case Enum:");
        sb.AppendLine("                return true;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return false;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static string FormatLeafValue(object? value)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (value == null) return \"[grey]null[/]\";");
        sb.AppendLine();
        sb.AppendLine("        if (value is INoValue { IsValue: false }) return \"[grey]no value[/]\";");
        sb.AppendLine();
        sb.AppendLine("        if (value is IIppValue ippVal)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!ippVal.IsValue) return \"[grey]no value[/]\";");
        sb.AppendLine("            return FormatLeafValue(ippVal.ValueAsObject);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (value is IppStatusCode statusCode)");
        sb.AppendLine("        {");
        sb.AppendLine("            var statusColor = GetStatusColor(statusCode);");
        sb.AppendLine("            return $\"[{statusColor}]{statusCode} (0x{(short)statusCode:X4})[/]\";");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (value is bool b) return b ? \"[green]true[/]\" : \"[red]false[/]\";");
        sb.AppendLine();
        sb.AppendLine("        if (value is byte[] bytes) return $\"[dim]{bytes.Length} bytes[/]\";");
        sb.AppendLine();
        sb.AppendLine("        if (value is IppAttribute attr)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (string.IsNullOrEmpty(attr.Name))");
        sb.AppendLine("                return $\"[dim]({attr.Tag}):[/] {Markup.Escape(attr.Value?.ToString() ?? \"null\")}\";");
        sb.AppendLine("            return $\"[bold]{Markup.Escape(attr.Name)}[/] [dim]({attr.Tag}):[/] {Markup.Escape(attr.Value?.ToString() ?? \"null\")}\";");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return Markup.Escape(value.ToString() ?? \"null\");");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private static string GetStatusColor(IppStatusCode statusCode)");
        sb.AppendLine("    {");
        sb.AppendLine("        var code = (short)statusCode;");
        sb.AppendLine("        if (code >= 0x0000 && code <= 0x00FF) return \"green\";");
        sb.AppendLine("        if (code >= 0x0100 && code <= 0x01FF) return \"yellow\";");
        sb.AppendLine("        if (code >= 0x0400 && code <= 0x04FF) return \"red\";");
        sb.AppendLine("        if (code >= 0x0500 && code <= 0x05FF) return \"red bold\";");
        sb.AppendLine("        return \"white\";");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static void RenderProperty(IHasTreeNodes parentNode, string key, object? value, HashSet<object> visited, int depth, HashSet<Type>? customLeafTypes = null)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (value == null) return;");
        sb.AppendLine();
        sb.AppendLine("        if (value is IIppValue ippVal)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!ippVal.IsValue)");
        sb.AppendLine("            {");
        sb.AppendLine("                parentNode.AddNode($\"[bold]{Markup.Escape(key)}:[/] [grey]no value[/]\");");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine("            var innerValue = ippVal.ValueAsObject;");
        sb.AppendLine("            if (innerValue == null)");
        sb.AppendLine("            {");
        sb.AppendLine("                parentNode.AddNode($\"[bold]{Markup.Escape(key)}:[/] [grey]null[/]\");");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine("            value = innerValue;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (IsLeaf(value, customLeafTypes))");
        sb.AppendLine("        {");
        sb.AppendLine("            parentNode.AddNode($\"[bold]{Markup.Escape(key)}:[/] {FormatLeafValue(value)}\");");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (value is IDictionary dict)");
        sb.AppendLine("        {");
        sb.AppendLine("            var dictNode = parentNode.AddNode($\"[bold]{Markup.Escape(key)}[/] [dim]({dict.Count})[/]\");");
        sb.AppendLine("            foreach (DictionaryEntry entry in dict)");
        sb.AppendLine("            {");
        sb.AppendLine("                var entryKey = entry.Key?.ToString() ?? \"null\";");
        sb.AppendLine("                if (entry.Value == null)");
        sb.AppendLine("                    dictNode.AddNode($\"[bold]{Markup.Escape(entryKey)}:[/] [grey]null[/]\");");
        sb.AppendLine("                else");
        sb.AppendLine("                    RenderProperty(dictNode, entryKey, entry.Value, visited, depth + 1, customLeafTypes);");
        sb.AppendLine("            }");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (value is IEnumerable enumerable and not string and not byte[])");
        sb.AppendLine("        {");
        sb.AppendLine("            RenderEnumerable(parentNode, key, enumerable, visited, depth, customLeafTypes);");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (!visited.Add(value))");
        sb.AppendLine("        {");
        sb.AppendLine("            parentNode.AddNode($\"[bold]{Markup.Escape(key)}:[/] [dim](circular reference)[/]\");");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        var objNode = parentNode.AddNode($\"[bold]{Markup.Escape(key)}[/]\");");
        sb.AppendLine("        RenderObjectProperties(objNode, value, visited, depth + 1, customLeafTypes);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private static void RenderEnumerable(IHasTreeNodes parentNode, string key, IEnumerable enumerable, HashSet<object> visited, int depth, HashSet<Type>? customLeafTypes)");
        sb.AppendLine("    {");
        sb.AppendLine("        var items = enumerable.Cast<object?>().Where(x => x != null).ToList();");
        sb.AppendLine("        if (items.Count == 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            parentNode.AddNode($\"[bold]{Markup.Escape(key)}:[/] [dim][[]][/]\");");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        var allLeaves = items.All(x => IsLeaf(x, customLeafTypes) && x is not IppAttribute);");
        sb.AppendLine("        if (allLeaves)");
        sb.AppendLine("        {");
        sb.AppendLine("            var formattedItems = items.Select(FormatLeafValue);");
        sb.AppendLine("            parentNode.AddNode($\"[bold]{Markup.Escape(key)}[/] [dim]({items.Count}):[/] {string.Join(\"[grey], [/]\", formattedItems)}\");");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        var listNode = parentNode.AddNode($\"[bold]{Markup.Escape(key)}[/] [dim]({items.Count})[/]\");");
        sb.AppendLine("        for (int i = 0; i < items.Count; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            var item = items[i]!;");
        sb.AppendLine("            if (item is IIppValue itemIppVal)");
        sb.AppendLine("            {");
        sb.AppendLine("                if (!itemIppVal.IsValue)");
        sb.AppendLine("                {");
        sb.AppendLine("                    listNode.AddNode($\"[dim][[{i}]][/] [grey]no value[/]\");");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                if (itemIppVal.ValueAsObject == null)");
        sb.AppendLine("                {");
        sb.AppendLine("                    listNode.AddNode($\"[dim][[{i}]][/] [grey]null[/]\");");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                item = itemIppVal.ValueAsObject;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            if (item is IppAttribute attr)");
        sb.AppendLine("            {");
        sb.AppendLine("                listNode.AddNode(FormatLeafValue(attr));");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (IsLeaf(item, customLeafTypes))");
        sb.AppendLine("            {");
        sb.AppendLine("                listNode.AddNode($\"[dim][[{i}]][/] {FormatLeafValue(item)}\");");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (item is IEnumerable innerEnumerable and not string and not byte[])");
        sb.AppendLine("            {");
        sb.AppendLine("                var itemNode = listNode.AddNode($\"[dim][[{i}]][/]\");");
        sb.AppendLine("                RenderEnumerableNodes(itemNode, innerEnumerable, visited, depth + 1, customLeafTypes);");
        sb.AppendLine("            }");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine("                if (!visited.Add(item))");
        sb.AppendLine("                {");
        sb.AppendLine("                    listNode.AddNode($\"[dim][[{i}]][/] [dim](circular reference)[/]\");");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                var itemNode = listNode.AddNode($\"[dim][[{i}]][/]\");");
        sb.AppendLine("                RenderObjectProperties(itemNode, item, visited, depth + 1, customLeafTypes);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private static void RenderEnumerableNodes(IHasTreeNodes parentNode, IEnumerable enumerable, HashSet<object> visited, int depth, HashSet<Type>? customLeafTypes = null)");
        sb.AppendLine("    {");
        sb.AppendLine("        var items = enumerable.Cast<object?>().Where(x => x != null).ToList();");
        sb.AppendLine("        if (items.Count == 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            parentNode.AddNode(\"[dim]Empty collection[/]\");");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        for (int i = 0; i < items.Count; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            var item = items[i]!;");
        sb.AppendLine("            if (item is IIppValue itemIppVal)");
        sb.AppendLine("            {");
        sb.AppendLine("                if (!itemIppVal.IsValue)");
        sb.AppendLine("                {");
        sb.AppendLine("                    parentNode.AddNode($\"[dim][[{i}]][/] [grey]no value[/]\");");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                if (itemIppVal.ValueAsObject == null)");
        sb.AppendLine("                {");
        sb.AppendLine("                    parentNode.AddNode($\"[dim][[{i}]][/] [grey]null[/]\");");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                item = itemIppVal.ValueAsObject;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            if (item is IppAttribute attr)");
        sb.AppendLine("            {");
        sb.AppendLine("                parentNode.AddNode(FormatLeafValue(attr));");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (IsLeaf(item, customLeafTypes))");
        sb.AppendLine("            {");
        sb.AppendLine("                parentNode.AddNode($\"[dim][[{i}]][/] {FormatLeafValue(item)}\");");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (item is IEnumerable innerEnumerable and not string and not byte[])");
        sb.AppendLine("            {");
        sb.AppendLine("                var itemNode = parentNode.AddNode($\"[dim][[{i}]][/]\");");
        sb.AppendLine("                RenderEnumerableNodes(itemNode, innerEnumerable, visited, depth + 1, customLeafTypes);");
        sb.AppendLine("            }");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine("                if (!visited.Add(item))");
        sb.AppendLine("                {");
        sb.AppendLine("                    parentNode.AddNode($\"[dim][[{i}]][/] [dim](circular reference)[/]\");");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                var itemNode = parentNode.AddNode($\"[dim][[{i}]][/]\");");
        sb.AppendLine("                RenderObjectProperties(itemNode, item, visited, depth + 1, customLeafTypes);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static void RenderObjectProperties(IHasTreeNodes parentNode, object obj, HashSet<object> visited, int depth, HashSet<Type>? customLeafTypes = null)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (depth > 20)");
        sb.AppendLine("        {");
        sb.AppendLine("            parentNode.AddNode(\"[dim]... (max depth reached)[/]\");");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (customLeafTypes == null && TryRenderStaticProperties(parentNode, obj, visited, depth))");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // Fallback for non-SharpIpp objects (e.g. arbitrary user models in tests)");
        sb.AppendLine("        var properties = obj.GetType()");
        sb.AppendLine("            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)");
        sb.AppendLine("            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)");
        sb.AppendLine("            .GroupBy(p => p.Name)");
        sb.AppendLine("            .Select(g => g.First())");
        sb.AppendLine("            .OrderBy(p => p.Name);");
        sb.AppendLine();
        sb.AppendLine("        foreach (var prop in properties)");
        sb.AppendLine("        {");
        sb.AppendLine("            var val = prop.GetValue(obj);");
        sb.AppendLine("            if (val == null) continue;");
        sb.AppendLine("            RenderProperty(parentNode, prop.Name, val, visited, depth + 1, customLeafTypes);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();

        // TryRenderStaticProperties dispatch method
        sb.AppendLine("    public static bool TryRenderStaticProperties(IHasTreeNodes parentNode, object obj, HashSet<object> visited, int depth)");
        sb.AppendLine("    {");
        sb.AppendLine("        switch (obj)");
        sb.AppendLine("        {");

        foreach (var type in candidateTypes)
        {
            var fullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            sb.AppendLine($"            case {fullName} x:");
            sb.AppendLine($"                RenderProperties(parentNode, x, visited, depth);");
            sb.AppendLine($"                return true;");
        }

        sb.AppendLine("            default:");
        sb.AppendLine("                return false;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Static RenderProperties overloads for each complex SharpIpp type
        foreach (var type in candidateTypes)
        {
            var fullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var properties = GetRenderableProperties(type);

            sb.AppendLine($"    public static void RenderProperties(IHasTreeNodes parentNode, {fullName} obj, HashSet<object> visited, int depth)");
            sb.AppendLine("    {");

            foreach (var prop in properties)
            {
                var isNonNullableValueType = prop.Type.IsValueType && !IsNullableValueType(prop.Type);

                if (isNonNullableValueType)
                {
                    sb.AppendLine($"        RenderProperty(parentNode, \"{prop.Name}\", obj.@{prop.Name}, visited, depth);");
                }
                else
                {
                    sb.AppendLine($"        if (obj.@{prop.Name} != null)");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            RenderProperty(parentNode, \"{prop.Name}\", obj.@{prop.Name}, visited, depth);");
                    sb.AppendLine("        }");
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");
        return sb.ToString();
    }
}
