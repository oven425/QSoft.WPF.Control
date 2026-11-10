using System.Globalization;

internal enum SrumOutputFormat
{
    Text,
    JsonLines,
    Csv,
    Xml
}

internal sealed record SrumOptions(
     string DatabasePath,
     SrumOutputFormat OutputFormat,
     uint MaxRows,
     int MaxOutputBytes,
     bool ShowHelp,
     bool SummaryMode,
     bool PowerCfgMode,
     string? TableName,
     string? OutputPath = null,
     int LockWaitSeconds = SrumCommandLine.DefaultLockWaitSeconds);

internal static class SrumCommandLine
{
    private const uint DefaultMaxRows = 20;
    private const uint AbsoluteMaxRows = 1000;
    private const int DefaultMaxOutputBytes = 4 * 1024 * 1024;
    private const int MinMaxOutputBytes = 1024;
    private const int AbsoluteMaxOutputBytes = 64 * 1024 * 1024;
    private const int AbsoluteMaxLockWaitSeconds = 3600;

    public const int DefaultLockWaitSeconds = 180;

    public static bool TryParse(string[] args, out SrumOptions options, out string? error)
    {
        string? databasePath = null;
        SrumOutputFormat outputFormat = SrumOutputFormat.Text;
        uint maxRows = DefaultMaxRows;
        int maxOutputBytes = DefaultMaxOutputBytes;
        bool showHelp = false;
        bool summaryMode = false;
        bool powerCfgMode = false;
        string? tableName = null;
        bool formatSpecified = false;
        string? outputPath = null;
        int? lockWaitSeconds = null;
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

                if (optionName is "--summary" or "--srumutil" or "--powercfg" or "--report" or "--csv" or "--xml")
                {
                    if (inlineValue is not null)
                    {
                        error = $"選項 {optionName} 不接受值。";
                        options = CreateOptions(databasePath, outputFormat, maxRows, maxOutputBytes, showHelp, summaryMode, powerCfgMode, tableName);
                        return false;
                    }

                    switch (optionName)
                    {
                        case "--summary":
                            summaryMode = true;
                            break;
                        case "--csv":
                            outputFormat = SrumOutputFormat.Csv;
                            formatSpecified = true;
                            break;
                        case "--xml":
                            outputFormat = SrumOutputFormat.Xml;
                            formatSpecified = true;
                            break;
                        default:
                            powerCfgMode = true;
                            break;
                    }

                    continue;
                }

                if (!TryReadValue(args, ref i, inlineValue, out string value))
                {
                    error = $"選項 {optionName} 缺少值。";
                    options = CreateOptions(databasePath, outputFormat, maxRows, maxOutputBytes, showHelp, summaryMode, powerCfgMode, tableName);
                    return false;
                }

                switch (optionName)
                {
                    case "--format" when TryParseFormat(value, out SrumOutputFormat parsedFormat):
                        outputFormat = parsedFormat;
                        formatSpecified = true;
                        break;
                    case "--format":
                        error = "--format 僅接受 text、jsonl、csv 或 xml。";
                        options = CreateOptions(databasePath, outputFormat, maxRows, maxOutputBytes, showHelp, summaryMode, powerCfgMode, tableName);
                        return false;
                    case "--max-rows" when uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out uint parsedRows) && parsedRows <= AbsoluteMaxRows:
                        maxRows = parsedRows;
                        break;
                    case "--max-rows":
                        error = $"--max-rows 必須介於 0 與 {AbsoluteMaxRows}。";
                        options = CreateOptions(databasePath, outputFormat, maxRows, maxOutputBytes, showHelp, summaryMode, powerCfgMode, tableName);
                        return false;
                    case "--max-output-bytes" when int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedBytes) && parsedBytes >= MinMaxOutputBytes && parsedBytes <= AbsoluteMaxOutputBytes:
                        maxOutputBytes = parsedBytes;
                        break;
                    case "--max-output-bytes":
                        error = $"--max-output-bytes 必須介於 {MinMaxOutputBytes} 與 {AbsoluteMaxOutputBytes}。";
                        options = CreateOptions(databasePath, outputFormat, maxRows, maxOutputBytes, showHelp, summaryMode, powerCfgMode, tableName);
                        return false;
                    case "--table" or "-t":
                        tableName = value;
                        break;
                    case "--output" when value.Length > 0:
                        outputPath = value;
                        break;
                    case "--output":
                        error = "--output 不可為空。";
                        options = CreateOptions(databasePath, outputFormat, maxRows, maxOutputBytes, showHelp, summaryMode, powerCfgMode, tableName);
                        return false;
                    case "--wait" when int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedWait) && parsedWait <= AbsoluteMaxLockWaitSeconds:
                        lockWaitSeconds = parsedWait;
                        break;
                    case "--wait":
                        error = $"--wait 必須介於 0 與 {AbsoluteMaxLockWaitSeconds} 秒。";
                        options = CreateOptions(databasePath, outputFormat, maxRows, maxOutputBytes, showHelp, summaryMode, powerCfgMode, tableName);
                        return false;
                    default:
                        error = $"不支援的選項：{optionName}。";
                        options = CreateOptions(databasePath, outputFormat, maxRows, maxOutputBytes, showHelp, summaryMode, powerCfgMode, tableName);
                        return false;
                }

                continue;
            }

            if (databasePath is not null)
            {
                error = "只能指定一個資料庫路徑。";
                options = CreateOptions(databasePath, outputFormat, maxRows, maxOutputBytes, showHelp, summaryMode, powerCfgMode, tableName);
                return false;
            }

            databasePath = argument;
        }

        if (!showHelp)
        {
            error = ValidateModes(powerCfgMode, summaryMode, tableName, formatSpecified, ref outputFormat, outputPath, lockWaitSeconds);
            if (error is not null)
            {
                options = CreateOptions(databasePath, outputFormat, maxRows, maxOutputBytes, showHelp, summaryMode, powerCfgMode, tableName);
                return false;
            }
        }

        options = CreateOptions(databasePath, outputFormat, maxRows, maxOutputBytes, showHelp, summaryMode, powerCfgMode, tableName, outputPath, lockWaitSeconds ?? DefaultLockWaitSeconds);
        return true;
    }

    public static string Usage => """
        用法：ConsoleApp_SrumDatabase [資料庫路徑] [選項]

        檢視資料庫：
          --format text|jsonl              輸出格式（預設 text）
          --max-rows 0..1000               每個資料表最多輸出幾列（預設 20）
          --max-output-bytes 1024..67108864
                                           輸出位元組上限（預設 4194304）
          --summary                        只列出各資料表的摘要
          --table <名稱>                   只輸出指定的資料表

        匯出與 powercfg /srumutil 相同格式的能源預估資料（--max-rows、--max-output-bytes 不適用）：
          --srumutil                       匯出 CSV 或 XML（別名 --powercfg）
          --csv | --xml                    選擇格式，預設 CSV；也可寫成 --format csv|xml
          --output <檔案>                  輸出檔，預設是目前目錄的 srumutil.csv 或 srumutil.xml
          --wait <秒>                      資料庫被 SRU 服務鎖住時最多等待幾秒（0..3600，預設 180）

          --help                           顯示本說明

        結束碼：0 成功；1 失敗；2 參數錯誤；3 匯出完成，但有資料列因內容異常被略過。
        """;

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

    public static string GetDefaultOutputPath(SrumOutputFormat format) =>
        format == SrumOutputFormat.Xml ? "srumutil.xml" : "srumutil.csv";

    private static bool TryParseFormat(string value, out SrumOutputFormat format)
    {
        SrumOutputFormat? parsed = value.ToLowerInvariant() switch
        {
            "text" => SrumOutputFormat.Text,
            "jsonl" => SrumOutputFormat.JsonLines,
            "csv" => SrumOutputFormat.Csv,
            "xml" => SrumOutputFormat.Xml,
            _ => null
        };

        format = parsed.GetValueOrDefault();
        return parsed.HasValue;
    }

    private static string? ValidateModes(
        bool exportMode,
        bool summaryMode,
        string? tableName,
        bool formatSpecified,
        ref SrumOutputFormat outputFormat,
        string? outputPath,
        int? lockWaitSeconds)
    {
        if (!exportMode)
        {
            if (outputFormat is SrumOutputFormat.Csv or SrumOutputFormat.Xml)
                return "csv / xml 格式只能搭配 --srumutil 使用。";
            if (outputPath is not null)
                return "--output 只能搭配 --srumutil 使用。";
            if (lockWaitSeconds is not null)
                return "--wait 只能搭配 --srumutil 使用。";

            return null;
        }

        if (summaryMode)
            return "--summary 不能與 --srumutil 併用。";
        if (tableName is not null)
            return "--table 不能與 --srumutil 併用；匯出的內容固定是能源預估資料。";

        if (!formatSpecified)
            outputFormat = SrumOutputFormat.Csv;
        else if (outputFormat is not (SrumOutputFormat.Csv or SrumOutputFormat.Xml))
            return "--srumutil 的輸出格式只能是 csv 或 xml。";

        return null;
    }

    private static SrumOptions CreateOptions(
          string? databasePath,
          SrumOutputFormat outputFormat,
          uint maxRows,
          int maxOutputBytes,
          bool showHelp,
          bool summaryMode,
          bool powerCfgMode,
          string? tableName,
          string? outputPath = null,
          int lockWaitSeconds = DefaultLockWaitSeconds) =>
          new(databasePath ?? DefaultDatabasePath, outputFormat, maxRows, maxOutputBytes, showHelp, summaryMode, powerCfgMode, tableName, outputPath, lockWaitSeconds);

    private const string DefaultDatabasePath = @"C:\Windows\System32\sru\SRUDB.dat";
}
