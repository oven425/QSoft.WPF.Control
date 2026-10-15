using System.Globalization;
using System.Text;
using System.Text.Json;

internal sealed record SrumColumnSchema(string Name, uint ColumnId, uint EsentType, string TypeName, uint MaxLength, uint CodePage);

internal sealed record SrumIndexKey(string IndexName, string ColumnName, uint ColumnId, uint KeyOrder, bool Descending);

internal sealed record SrumIndexSchema(string Name, uint Flags, IReadOnlyList<SrumIndexKey> KeyColumns);

internal sealed class SrumReportWriter
{
    private static readonly UTF8Encoding Utf8 = new(false);
    private readonly SrumOutputFormat _format;
    private readonly int _maximumOutputBytes;
    private readonly byte[] _truncationLine;
    private readonly int _newlineBytes;
    private int _bytesWritten;
    private bool _truncated;

    public bool IsTruncated => _truncated;

    public SrumReportWriter(SrumOutputFormat format, int maximumOutputBytes)
    {
        _format = format;
        _maximumOutputBytes = maximumOutputBytes;
        Console.OutputEncoding = Utf8;
        _newlineBytes = Utf8.GetByteCount(Console.Out.NewLine);
        _truncationLine = format == SrumOutputFormat.JsonLines
            ? SerializeJsonLine("truncated", writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("reason", "maximum-output-bytes");
                writer.WriteNumber("limit", maximumOutputBytes);
                writer.WriteEndObject();
            })
            : Utf8.GetBytes("輸出已截斷：達到 --max-output-bytes 限制。");
    }

    public void WriteDatabase(uint databaseId, uint tableCount) =>
        Write("database", $"資料庫：{databaseId}；資料表數：{tableCount}", writer =>
        {
            writer.WriteStartObject();
            writer.WriteNumber("databaseId", databaseId);
            writer.WriteNumber("tableCount", tableCount);
            writer.WriteEndObject();
        });

    public void WriteTable(string tableName, int columnCount, int indexCount) =>
        Write("table", $"[{tableName}]（欄位 {columnCount}，索引 {indexCount}；索引不是外鍵）", writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("name", tableName);
            writer.WriteNumber("columnCount", columnCount);
            writer.WriteNumber("indexCount", indexCount);
            writer.WriteString("indexSemantics", "ESENT index metadata; not a foreign key");
            writer.WriteEndObject();
        });

    public void WriteColumn(string tableName, SrumColumnSchema column, IReadOnlyList<SrumIndexKey> indexKeys) =>
        Write("column", $"  {column.Name,-40} {column.TypeName,-16} type={column.EsentType} id={column.ColumnId} max={column.MaxLength} bytes cp={column.CodePage}{FormatColumnIndexKeys(indexKeys)}", writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("table", tableName);
            writer.WriteString("name", column.Name);
            writer.WriteNumber("columnId", column.ColumnId);
            writer.WriteNumber("esentTypeValue", column.EsentType);
            writer.WriteString("esentType", column.TypeName);
            writer.WriteNumber("maxLengthBytes", column.MaxLength);
            writer.WriteNumber("codePage", column.CodePage);
            writer.WriteStartArray("indexKeys");
            foreach (SrumIndexKey key in indexKeys)
            {
                writer.WriteStartObject();
                writer.WriteString("index", key.IndexName);
                writer.WriteNumber("keyOrder", key.KeyOrder + 1);
                writer.WriteString("direction", key.Descending ? "descending" : "ascending");
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        });

    public void WriteRecord(string tableName, uint rowNumber, IReadOnlyList<SrumDecodedValue> values) =>
        Write("record", $"{tableName} row {rowNumber}: {string.Join(" | ", values.Select(FormatValue))}", writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("table", tableName);
            writer.WriteNumber("rowNumber", rowNumber);
            writer.WriteStartArray("values");
            foreach (SrumDecodedValue value in values)
            {
                writer.WriteStartObject();
                writer.WriteString("name", value.Name);
                writer.WriteNumber("columnId", value.ColumnId);
                writer.WriteNumber("esentTypeValue", value.EsentType);
                writer.WriteString("esentType", value.TypeName);
                writer.WriteString("status", value.Status);
                writer.WriteNumber("byteLength", value.ByteLength);
                WriteValue(writer, value.Value);
                if (value.PreviewHex is not null)
                    writer.WriteString("previewHex", value.PreviewHex);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        });

    public void WriteIndex(string tableName, SrumIndexSchema index) =>
        Write("index", $"  索引 {index.Name} flags=0x{index.Flags:X8}: {FormatIndexKeys(index.KeyColumns)}", writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("table", tableName);
            writer.WriteString("name", index.Name);
            writer.WriteNumber("flags", index.Flags);
            writer.WriteString("semantics", "ESENT index metadata; not a foreign key");
            writer.WriteStartArray("keyColumns");
            foreach (SrumIndexKey key in index.KeyColumns)
            {
                writer.WriteStartObject();
                writer.WriteNumber("keyOrder", key.KeyOrder + 1);
                writer.WriteString("name", key.ColumnName);
                writer.WriteNumber("columnId", key.ColumnId);
                writer.WriteString("direction", key.Descending ? "descending" : "ascending");
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        });

    private void Write(string eventType, string text, Action<Utf8JsonWriter> writeData)
    {
        if (_truncated)
            return;

        byte[] jsonLine = _format == SrumOutputFormat.JsonLines
            ? SerializeJsonLine(eventType, writeData)
            : [];
        int lineBytes = _format == SrumOutputFormat.JsonLines ? jsonLine.Length : Utf8.GetByteCount(text);
        if (_bytesWritten + lineBytes + _newlineBytes + _truncationLine.Length + _newlineBytes > _maximumOutputBytes)
        {
            WriteTruncationLine();
            return;
        }

        if (_format == SrumOutputFormat.JsonLines)
            Console.WriteLine(Utf8.GetString(jsonLine));
        else
            Console.WriteLine(text);

        _bytesWritten += lineBytes + _newlineBytes;
    }

    private void WriteTruncationLine()
    {
        _truncated = true;
        if (_bytesWritten + _truncationLine.Length + _newlineBytes > _maximumOutputBytes)
        {
            Console.Error.WriteLine("輸出已截斷：達到 --max-output-bytes 限制。");
            return;
        }

        Console.WriteLine(Utf8.GetString(_truncationLine));
        _bytesWritten += _truncationLine.Length + _newlineBytes;
    }

    private static byte[] SerializeJsonLine(string eventType, Action<Utf8JsonWriter> writeData)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("type", eventType);
            writer.WritePropertyName("data");
            writeData(writer);
            writer.WriteEndObject();
        }

        return buffer.ToArray();
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNull("value");
                break;
            case bool boolean:
                writer.WriteBoolean("value", boolean);
                break;
            case byte number:
                writer.WriteNumber("value", number);
                break;
            case short number:
                writer.WriteNumber("value", number);
                break;
            case ushort number:
                writer.WriteNumber("value", number);
                break;
            case int number:
                writer.WriteNumber("value", number);
                break;
            case uint number:
                writer.WriteNumber("value", number);
                break;
            case long number:
                writer.WriteNumber("value", number);
                break;
            case decimal number:
                writer.WriteNumber("value", number);
                break;
            case float number when float.IsFinite(number):
                writer.WriteNumber("value", number);
                break;
            case double number when double.IsFinite(number):
                writer.WriteNumber("value", number);
                break;
            case string text:
                writer.WriteString("value", text);
                break;
            default:
                writer.WriteString("value", Convert.ToString(value, CultureInfo.InvariantCulture));
                break;
        }
    }

    private static string FormatValue(SrumDecodedValue value)
    {
        if (value.Status == "null")
            return $"{value.Name}=<NULL>";

        if (value.Value is not null)
            return $"{value.Name}={Convert.ToString(value.Value, CultureInfo.InvariantCulture)}";

        string preview = value.PreviewHex is null ? string.Empty : $", hex={value.PreviewHex}";
        return $"{value.Name}=<{value.Status}; {value.ByteLength} bytes{preview}>";
    }

    private static string FormatIndexKeys(IReadOnlyList<SrumIndexKey> indexKeys) =>
        string.Join(", ", indexKeys
            .OrderBy(key => key.KeyOrder)
            .Select(key => $"{key.ColumnName}#{key.KeyOrder + 1} {(key.Descending ? "DESC" : "ASC")}"));

    private static string FormatColumnIndexKeys(IReadOnlyList<SrumIndexKey> indexKeys) =>
        string.Join(", ", indexKeys
            .OrderBy(key => key.KeyOrder)
            .Select(key => $"{key.IndexName}#{key.KeyOrder + 1} {(key.Descending ? "DESC" : "ASC")}"));
}
