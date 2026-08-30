using Microsoft.CodeAnalysis;

namespace IppCli.Generators;

/// <summary>
/// Diagnostic descriptors emitted by the IPP CLI settings source generator.
/// </summary>
public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor ClassMustBePartial = new(
        id: "IPPCLI001",
        title: "Class decorated with [GenerateCliSettings] must be partial",
        messageFormat: "The class '{0}' decorated with [GenerateCliSettings] is not declared as partial",
        category: "CliSettingsGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PropertyNotFound = new(
        id: "IPPCLI002",
        title: "Target request property not found",
        messageFormat: "Property '{0}' specified in [GenerateCliSettings] on class '{1}' was not found or is not an accessible public instance property",
        category: "CliSettingsGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnmappedPropertyFallback = new(
        id: "IPPCLI003",
        title: "Property type fallback without setter mapping",
        messageFormat: "Property '{0}' on '{1}' has type '{2}' which cannot be bound to a CLI option and has no setter mapping",
        category: "CliSettingsGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidMaxNestingDepth = new(
        id: "IPPCLI004",
        title: "Invalid MaxNestingDepth",
        messageFormat: "MaxNestingDepth '{0}' on class '{1}' must be greater than or equal to 0",
        category: "CliSettingsGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
