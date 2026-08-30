using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace IppCli.Generators;

/// <summary>
/// Model containing semantic analysis target information and diagnostics for CLI settings code generation.
/// </summary>
internal readonly struct TargetInfo
{
    public INamedTypeSymbol ClassSymbol { get; }
    public INamedTypeSymbol? RequestTypeSymbol { get; }
    public string PropertyName { get; }
    public int MaxNestingDepth { get; }
    public bool IsPartial { get; }
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    public TargetInfo(
        INamedTypeSymbol classSymbol,
        INamedTypeSymbol? requestTypeSymbol,
        string propertyName,
        int maxNestingDepth,
        bool isPartial,
        ImmutableArray<Diagnostic> diagnostics)
    {
        ClassSymbol = classSymbol;
        RequestTypeSymbol = requestTypeSymbol;
        PropertyName = propertyName;
        MaxNestingDepth = maxNestingDepth;
        IsPartial = isPartial;
        Diagnostics = diagnostics;
    }
}
