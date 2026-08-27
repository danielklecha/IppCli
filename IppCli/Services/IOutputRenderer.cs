using System.IO;
using SharpIpp.Protocol;

namespace IppCli.Services;

/// <summary>
/// Defines a unified contract for rendering IPP responses to console or text streams.
/// </summary>
public interface IOutputRenderer
{
    /// <summary>
    /// Renders an IPP response with the specified operation name to the provided TextWriter or default console.
    /// </summary>
    /// <param name="operationName">The IPP operation name (e.g., Get-Printer-Attributes).</param>
    /// <param name="response">The IPP response object.</param>
    /// <param name="writer">Optional TextWriter destination (defaults to Console or internal renderer sink).</param>
    void RenderResponse(string operationName, IIppResponse response, TextWriter? writer = null);
}
