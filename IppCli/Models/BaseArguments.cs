namespace IppCli.Models;

public class BaseArguments
{
    public OutputFormat Output { get; set; } = OutputFormat.Tree;

    public static BaseArguments Parse(string[]? args)
    {
        var result = new BaseArguments();

        if (args == null || args.Length == 0)
        {
            return result;
        }

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.IsNullOrWhiteSpace(arg))
            {
                continue;
            }

            // Check for -o <val> or --output <val>
            if (string.Equals(arg, "-o", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--output", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal))
                {
                    result.Output = ParseFormat(args[i + 1]);
                }
            }
        }

        return result;
    }

    public static OutputFormat GetOutputFormat(string[]? args) => Parse(args).Output;

    private static OutputFormat ParseFormat(string? value)
    {
        if (Enum.TryParse<OutputFormat>(value?.Trim(), ignoreCase: true, out var format))
        {
            return format;
        }

        return OutputFormat.Tree;
    }
}
