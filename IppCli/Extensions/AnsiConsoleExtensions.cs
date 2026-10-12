using System.Net.Sockets;
using System.Security.Authentication;
using IppCli.Models;
using IppCli.Serialization;
using SharpIpp.Exceptions;
using SharpIpp.Protocol.Models;
using Spectre.Console;

namespace IppCli.Extensions;

public static class AnsiConsoleExtensions
{
    public static void WriteException(this IAnsiConsole console, Exception ex) =>
        WriteException(console, ex, OutputFormat.Tree);

    public static void WriteException(this IAnsiConsole console, Exception ex, OutputFormat format)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(ex);

        if (format == OutputFormat.Json)
        {
            var json = FormatExceptionJson(ex);
            console.WriteLine(json);
            return;
        }

        var tree = CreateExceptionTree(ex);
        console.Write(tree);

        if (string.Equals(Environment.GetEnvironmentVariable("IPP_CLI_DEBUG"), "1", StringComparison.OrdinalIgnoreCase))
        {
            console.WriteLine();
            Spectre.Console.AnsiConsoleExtensions.WriteException(console, ex, ExceptionFormats.ShortenEverything);
        }
    }

    public static void WriteException(Exception ex, OutputFormat format = OutputFormat.Tree, IAnsiConsole? console = null)
    {
        ArgumentNullException.ThrowIfNull(ex);
        (console ?? CreateErrorConsole()).WriteException(ex, format);
    }

    public static void WriteException(Exception ex, IAnsiConsole? console) =>
        WriteException(ex, OutputFormat.Tree, console);

    public static void WriteExceptions(this IAnsiConsole console, IEnumerable<Exception> exceptions) =>
        WriteExceptions(console, exceptions, OutputFormat.Tree);

    public static void WriteExceptions(this IAnsiConsole console, IEnumerable<Exception> exceptions, OutputFormat format)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(exceptions);

        var list = exceptions as IReadOnlyList<Exception> ?? exceptions.ToList();
        if (list.Count == 0)
        {
            return;
        }

        if (format == OutputFormat.Json)
        {
            var json = FormatExceptionsJson(list);
            console.WriteLine(json);
            return;
        }

        var tree = CreateExceptionsTree(list);
        if (tree != null)
        {
            console.Write(tree);
        }
    }

    public static void WriteExceptions(IEnumerable<Exception> exceptions, OutputFormat format = OutputFormat.Tree, IAnsiConsole? console = null)
    {
        ArgumentNullException.ThrowIfNull(exceptions);
        (console ?? CreateErrorConsole()).WriteExceptions(exceptions, format);
    }

    public static void WriteExceptions(IEnumerable<Exception> exceptions, IAnsiConsole? console) =>
        WriteExceptions(exceptions, OutputFormat.Tree, console);

    public static string FormatExceptionJson(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        var responseMessage = FindIppResponseMessage(ex);
        if (responseMessage != null)
        {
            return IppJsonHelper.SerializeOutput(responseMessage);
        }

        return IppJsonHelper.SerializeOutput(FormatExceptionDto(ex));
    }

    public static string FormatExceptionsJson(IEnumerable<Exception> exceptions)
    {
        ArgumentNullException.ThrowIfNull(exceptions);

        var list = exceptions as IReadOnlyList<Exception> ?? exceptions.ToList();
        if (list.Count == 0)
        {
            return string.Empty;
        }

        var errors = list.Select(ex =>
        {
            var responseMessage = FindIppResponseMessage(ex);
            if (responseMessage != null)
            {
                return (object)responseMessage;
            }

            return FormatExceptionDto(ex);
        }).ToList();

        return IppJsonHelper.SerializeOutput(errors);
    }

    private static object FormatExceptionDto(Exception ex)
    {
        var requestMessage = ex is IppRequestException { RequestMessage: not null } reqEx
            ? reqEx.RequestMessage
            : (ex.InnerException == null && ex is not AggregateException ? FindIppRequestMessage(ex) : null);

        if (ex is AggregateException agg)
        {
            var flattened = agg.Flatten();
            var innerExceptions = flattened.InnerExceptions.Select(FormatExceptionDto).ToList();
            if (requestMessage != null)
            {
                return new
                {
                    error = nameof(AggregateException),
                    message = agg.Message,
                    innerExceptions,
                    requestMessage
                };
            }

            return new
            {
                error = nameof(AggregateException),
                message = agg.Message,
                innerExceptions
            };
        }

        if (ex.InnerException != null)
        {
            var innerException = FormatExceptionDto(ex.InnerException);
            if (requestMessage != null)
            {
                return new
                {
                    error = ex.GetType().Name,
                    message = ex.Message,
                    innerException,
                    requestMessage
                };
            }

            return new
            {
                error = ex.GetType().Name,
                message = ex.Message,
                innerException
            };
        }

        if (requestMessage != null)
        {
            return new
            {
                error = ex.GetType().Name,
                message = ex.Message,
                requestMessage
            };
        }

        return new
        {
            error = ex.GetType().Name,
            message = ex.Message
        };
    }

    private static SharpIpp.Protocol.IIppResponseMessage? FindIppResponseMessage(Exception ex) =>
        EnumerateExceptions(ex).OfType<IppResponseException>().FirstOrDefault(e => e.ResponseMessage != null)?.ResponseMessage;

    private static SharpIpp.Protocol.IIppRequestMessage? FindIppRequestMessage(Exception ex) =>
        EnumerateExceptions(ex).OfType<IppRequestException>().FirstOrDefault(e => e.RequestMessage != null)?.RequestMessage;

    public static Tree CreateExceptionTree(this Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        Tree tree;
        if (ex is AggregateException agg)
        {
            var flattened = agg.Flatten();
            tree = new Tree($"[bold red]Error[/] [dim red](AggregateException):[/] [red]{Markup.Escape(agg.Message)}[/]");
            foreach (var inner in flattened.InnerExceptions)
            {
                if (inner != null)
                {
                    AddExceptionNode(tree, inner);
                }
            }
        }
        else
        {
            var rootMarkup = $"[bold red]Error[/] [dim red]({Markup.Escape(ex.GetType().Name)}):[/] [red]{Markup.Escape(ex.Message)}[/]";
            tree = new Tree(rootMarkup);

            AttachSharpIppDetails(tree, ex);

            if (ex.InnerException != null)
            {
                AddExceptionNode(tree, ex.InnerException);
            }
        }

        return tree;
    }

    public static Tree? CreateExceptionsTree(this IEnumerable<Exception> exceptions)
    {
        ArgumentNullException.ThrowIfNull(exceptions);

        var list = exceptions as IReadOnlyList<Exception> ?? exceptions.ToList();
        if (list.Count == 0)
        {
            return null;
        }

        var tree = new Tree("[bold red]Errors[/]");
        foreach (var ex in list)
        {
            if (ex != null)
            {
                AddExceptionNode(tree, ex);
            }
        }

        return tree;
    }

    public static void WriteHints(this IAnsiConsole console, Exception ex)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(ex);

        if (IsSslError(ex))
        {
            console.MarkupLine("[dim yellow]Hint: If the printer uses a self-signed certificate, retry with [bold]-k[/] or [bold]--insecure[/].[/]");
            return;
        }

        if (IsTimeoutError(ex))
        {
            console.MarkupLine("[dim yellow]Hint: The request timed out. You can increase the timeout with [bold]-t <seconds>[/] or [bold]--timeout <seconds>[/].[/]");
            return;
        }

        if (IsSocketError(ex))
        {
            console.MarkupLine("[dim yellow]Hint: Check that the printer URI and port are correct and that the IPP service is reachable.[/]");
        }
    }

    public static void WriteHints(Exception ex, IAnsiConsole? console = null)
    {
        ArgumentNullException.ThrowIfNull(ex);
        (console ?? CreateErrorConsole()).WriteHints(ex);
    }

    public static IAnsiConsole CreateErrorConsole() =>
        AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(Console.Error)
        });

    private static TreeNode AddExceptionNode(IHasTreeNodes parent, Exception ex)
    {
        var nodeMarkup = $"[dim red]({Markup.Escape(ex.GetType().Name)}):[/] [red]{Markup.Escape(ex.Message)}[/]";
        var node = parent.AddNode(nodeMarkup);

        AttachSharpIppDetails(node, ex);

        if (ex is AggregateException agg)
        {
            var flattened = agg.Flatten();
            foreach (var inner in flattened.InnerExceptions)
            {
                if (inner != null)
                {
                    AddExceptionNode(node, inner);
                }
            }
        }
        else if (ex.InnerException != null)
        {
            AddExceptionNode(node, ex.InnerException);
        }

        return node;
    }

    private static void AttachSharpIppDetails(IHasTreeNodes node, Exception ex)
    {
        switch (ex)
        {
            case IppResponseException { ResponseMessage: not null } ippRespEx:
                node.AddNode(ConsoleTreeRenderer.CreateTree(ippRespEx.ResponseMessage, "ResponseMessage"));
                return;

            case IppRequestException { RequestMessage: not null } ippReqEx:
                node.AddNode(ConsoleTreeRenderer.CreateTree(ippReqEx.RequestMessage, "RequestMessage"));
                return;
        }

        if (ex.GetType().Namespace?.StartsWith("SharpIpp", StringComparison.Ordinal) == true)
        {
            var properties = ex.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
            foreach (var prop in properties)
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                var value = prop.GetValue(ex);
                if (value != null && value is not Enum && value.GetType().Namespace?.StartsWith("SharpIpp", StringComparison.Ordinal) == true)
                {
                    node.AddNode(ConsoleTreeRenderer.CreateTree(value, prop.Name));
                }
            }
        }
    }

    public static void WriteSharpIppDetails(this IAnsiConsole console, Exception ex)
    {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(ex);

        switch (ex)
        {
            case IppResponseException { ResponseMessage: not null } ippRespEx:
                ConsoleTreeRenderer.Render(ippRespEx.ResponseMessage, "ResponseMessage", null, console);
                return;

            case IppRequestException { RequestMessage: not null } ippReqEx:
                ConsoleTreeRenderer.Render(ippReqEx.RequestMessage, "RequestMessage", null, console);
                return;
        }

        if (ex.GetType().Namespace?.StartsWith("SharpIpp", StringComparison.Ordinal) == true)
        {
            var properties = ex.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
            foreach (var prop in properties)
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                var value = prop.GetValue(ex);
                if (value != null && value is not Enum && value.GetType().Namespace?.StartsWith("SharpIpp", StringComparison.Ordinal) == true)
                {
                    ConsoleTreeRenderer.Render(value, prop.Name, null, console);
                }
            }
        }
    }

    public static void WriteIppResponseMessage(this IAnsiConsole console, Exception ex) =>
        console.WriteSharpIppDetails(ex);

    private static bool IsSslError(Exception ex) =>
        EnumerateExceptions(ex).Any(e =>
            e is AuthenticationException
            || e.Message.Contains("certificate", StringComparison.OrdinalIgnoreCase));

    private static bool IsTimeoutError(Exception ex) =>
        EnumerateExceptions(ex).Any(e => e is TaskCanceledException or TimeoutException);

    private static bool IsSocketError(Exception ex) =>
        EnumerateExceptions(ex).Any(e => e is SocketException);

    private static IEnumerable<Exception> EnumerateExceptions(Exception ex)
    {
        var visited = new HashSet<Exception>(ReferenceEqualityComparer.Instance);
        var queue = new Queue<Exception>();
        queue.Enqueue(ex);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }

            yield return current;

            if (current is AggregateException agg)
            {
                foreach (var inner in agg.InnerExceptions)
                {
                    if (inner != null)
                    {
                        queue.Enqueue(inner);
                    }
                }
            }

            if (current.InnerException != null)
            {
                queue.Enqueue(current.InnerException);
            }
        }
    }
}
