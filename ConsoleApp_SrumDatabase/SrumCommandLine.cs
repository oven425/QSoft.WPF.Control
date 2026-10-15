using System.Globalization;

internal enum SrumOutputFormat
{
    Text,
    JsonLines
}

internal sealed record SrumOptions(
    string DatabasePath,
    SrumOutputFormat OutputFormat,
    uint MaxRows,
    int MaxOutputBytes,
    bool ShowHelp);

internal static class SrumCommandLine
{
    private const uint DefaultMaxRows = 20;
    private const uint AbsoluteMaxRows = 1000;
    private const int DefaultMaxOutputBytes = 4 * 1024 * 1024;
    private const int MinMaxOutputBytes = 1024;
    private const int AbsoluteMaxOutputBytes = 64 * 1024 * 1024;

    public static bool TryParse(string[] args, out SrumOptions options, out string? error)
    {
        string? databasePath = null;
        SrumOutputFormat outputFormat = SrumOutputFormat.Text;
        uint maxRows = DefaultMaxRows;
        int maxOutputBytes = DefaultMaxOutputBytes;
        bool showHelp = false;
        error = null;

        for (int i = 0; i < args.Length; i++)
        {
            string argument = args[i];
            if (argument is "--help" or "-h")
            {
                showHelp = true;
                continue;
            }

            if (argument.StartsWith("--", StringComparison.Ordinal))
            {
                int equalsIndex = argument.IndexOf('=');
                string optionName = equalsIndex < 0 ? argument : argument[..equalsIndex];
                string? inlineValue = equalsIndex < 0 ? null : argument[(equalsIndex + 1)..];

                if (!TryReadValue(args, ref i, inlineValue, out string value))
                {
                    error = $"選項 {optionName} 缺少值。";
                    options = CreateOptions(databasePath, outputFormat, maxRows, maxOutputBytes, showHelp);
                    return false;
                }

                switch (optionName)
                {
                    case "--format" when value.Equals("text", StringComparison.OrdinalIgnoreCase):
                        outputFormat = SrumOutputFormat.Text;
                        break;
                    case "--format" when value.Equals("jsonl", StringComparison.OrdinalIgnoreCase):
                        outputFormat = SrumOutputFormat.JsonLines;
                        break;
                    case "--format":
                        error = "--format 僅接受 text 或 jsonl。";
                        options = CreateOptions(databasePath, outputFormat, maxRows, maxOutputBytes, showHelp);
                        return false;
                    case "--max-rows" when uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out uint parsedRows) && parsedRows <= AbsoluteMaxRows:
                        maxRows = parsedRows;
                        break;
                    case "--max-rows":
                        error = $"--max-rows 必須介於 0 與 {AbsoluteMaxRows}。";
                        options = CreateOptions(databasePath, outputFormat, maxRows, maxOutputBytes, showHelp);
                        return false;
                    case "--max-output-bytes" when int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedBytes) && parsedBytes >= MinMaxOutputBytes && parsedBytes <= AbsoluteMaxOutputBytes:
                        maxOutputBytes = parsedBytes;
                        break;
                    case "--max-output-bytes":
                        error = $"--max-output-bytes 必須介於 {MinMaxOutputBytes} 與 {AbsoluteMaxOutputBytes}。";
                        options = CreateOptions(databasePath, outputFormat, maxRows, maxOutputBytes, showHelp);
                        return false;
                    default:
                        error = $"不支援的選項：{optionName}。";
                        options = CreateOptions(databasePath, outputFormat, maxRows, maxOutputBytes, showHelp);
                        return false;
                }

                continue;
            }

            if (databasePath is not null)
            {
                error = "只能指定一個資料庫路徑。";
                options = CreateOptions(databasePath, outputFormat, maxRows, maxOutputBytes, showHelp);
                return false;
            }

            databasePath = argument;
        }

        options = CreateOptions(databasePath, outputFormat, maxRows, maxOutputBytes, showHelp);
        return true;
    }

    public static string Usage =>
        "用法：ConsoleApp_SrumDatabase [資料庫路徑] [--format text|jsonl] [--max-rows 0..1000] [--max-output-bytes 1024..67108864] [--help]";

    private static bool TryReadValue(string[] args, ref int index, string? inlineValue, out string value)
    {
        if (inlineValue is not null)
        {
            value = inlineValue;
            return true;
        }

        if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            value = args[++index];
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static SrumOptions CreateOptions(
        string? databasePath,
        SrumOutputFormat outputFormat,
        uint maxRows,
        int maxOutputBytes,
        bool showHelp) =>
        new(databasePath ?? DefaultDatabasePath, outputFormat, maxRows, maxOutputBytes, showHelp);

    private const string DefaultDatabasePath = @"C:\Windows\System32\sru\SRUDB.dat";
}
