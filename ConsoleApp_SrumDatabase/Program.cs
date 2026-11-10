
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

if (!SrumCommandLine.TryParse(args, out SrumOptions options, out string? argumentError))
{
    Console.Error.WriteLine(argumentError);
    Console.Error.WriteLine(SrumCommandLine.Usage);
    return 2;
}

if (options.ShowHelp)
{
    Console.WriteLine(SrumCommandLine.Usage);
    return 0;
}

if (!OperatingSystem.IsWindows())
{
    Console.Error.WriteLine("ESENT 僅能在 Windows 上使用。");
    return 1;
}

string databasePath = Path.GetFullPath(options.DatabasePath);

if (!File.Exists(databasePath))
{
    Console.Error.WriteLine($"找不到資料庫：{databasePath}");
    return 1;
}

try
{
    if (options.PowerCfgMode)
    {
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancellation.Cancel();
        };

        string outputPath = Path.GetFullPath(options.OutputPath ?? SrumCommandLine.GetDefaultOutputPath(options.OutputFormat));
        SrumUtilExportResult result = SrumUtilExporter.Export(databasePath, outputPath, options.OutputFormat, TimeSpan.FromSeconds(options.LockWaitSeconds), cancellation.Token);
        Console.WriteLine($"已將 {result.RecordCount} 筆能源預估記錄以 {options.OutputFormat.ToString().ToUpperInvariant()} 格式寫入 {result.OutputPath}");
        Console.WriteLine("（不含 SRU 服務尚未寫入資料庫的最新記錄；powercfg 會向服務查詢這部分。）");
        return result.SkippedCount > 0 ? 3 : 0;
    }

    var output = new SrumReportWriter(options.OutputFormat, options.MaxOutputBytes);
    if (options.SummaryMode)
    {
        SrumSchemaReader.PrintSummary(databasePath, options.TableName, output);
        return 0;
    }

    if (options.TableName is not null)
    {
        SrumSchemaReader.PrintTable(databasePath, options.TableName, options.MaxRows, output);
        return 0;
    }

    SrumSchemaReader.Print(databasePath, options.MaxRows, output);
    return 0;
}
catch (UnauthorizedAccessException)
{
    Console.Error.WriteLine("沒有讀取資料庫的權限；請以具有適當權限的帳戶執行。");
    return 1;
}
catch (EsentException ex) when (ex.ErrorCode == EsentNative.JetErrFileAccessDenied)
{
    Console.Error.WriteLine("資料庫目前被其他程序（通常是 SRU 服務）使用中，無法開啟；請稍後再試。使用 --srumutil 時可用 --wait 延長等待時間。");
    return 1;
}
catch (EsentException ex)
{
    Console.Error.WriteLine($"{ex.Operation} 失敗，ESENT 錯誤碼 {ex.ErrorCode} (0x{unchecked((uint)ex.ErrorCode):X8})。");
    Console.Error.WriteLine("資料庫可能正由 SRU 服務使用，或需要交易記錄才能復原。請改用一致性副本（包含相符的 ESE 記錄檔），不要直接複製正在使用中的 SRUDB.dat。");
    return 1;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("已取消。");
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

internal sealed class EsentException(string operation, int errorCode)
    : Exception($"{operation} 失敗，ESENT 錯誤碼 {errorCode}。")
{
    public string Operation { get; } = operation;
    public int ErrorCode { get; } = errorCode;
}

internal sealed record SrumTableSchema(string Name, IReadOnlyList<SrumColumnSchema> Columns, IReadOnlyList<SrumIndexSchema> Indexes);

internal sealed record SrumTableSummary(string Name, int ColumnCount, int IndexCount, ulong RowCount);

internal sealed record SrumIdMapEntry(int Index, byte Type, byte[]? Blob);

internal sealed record SrumProviderRecord(int AutoIncId, double TimeStampOaDate, int AppId, int UserId, byte[]? Data);

internal sealed class SrumReader : IDisposable
{
    private const uint MaxValueBytes = 1024 * 1024;

    private readonly string _databasePath;
    private readonly string _workDirectory;
    private readonly nint _instance;
    private readonly nint _sessionId;
    private readonly uint _databaseId;
    private bool _disposed;

    private SrumReader(string databasePath, string workDirectory, nint instance, nint sessionId, uint databaseId)
    {
        _databasePath = databasePath;
        _workDirectory = workDirectory;
        _instance = instance;
        _sessionId = sessionId;
        _databaseId = databaseId;
    }

    // SRU 服務只會在寫入期間（約數分鐘）附加 SRUDB.dat，其餘時間檔案是自由的，所以被鎖住時要等待而不是直接失敗。
    public static SrumReader Open(string databasePath, TimeSpan lockWaitTimeout = default, CancellationToken cancellationToken = default)
    {
        string workDirectory = Path.Combine(Path.GetTempPath(), $"SrumReader-{Guid.NewGuid():N}");
        nint instance = 0;
        nint sessionId = 0;
        bool instanceCreated = false;
        bool sessionStarted = false;
        bool databaseAttached = false;

        try
        {
            string systemPath = CreateDirectory(workDirectory, "system");
            string tempPath = CreateDirectory(workDirectory, "temp");
            string logPath = CreateDirectory(workDirectory, "logs");

            Check(EsentNative.JetCreateInstance(out instance, "SrumReader"), "建立 ESENT instance");
            instanceCreated = true;
            SetPath(ref instance, EsentNative.JetParamSystemPath, systemPath);
            SetPath(ref instance, EsentNative.JetParamTempPath, tempPath);
            SetPath(ref instance, EsentNative.JetParamLogFilePath, logPath);
            Check(EsentNative.JetSetSystemParameter(ref instance, 0, EsentNative.JetParamBaseName, 0, "SRU"), "設定 ESENT 檔名前綴");
            Check(EsentNative.JetInit(ref instance), "初始化 ESENT instance");
            Check(EsentNative.JetBeginSession(instance, out sessionId, null, null), "建立 ESENT session");
            sessionStarted = true;

            AttachReadOnly(sessionId, databasePath, lockWaitTimeout, cancellationToken);
            databaseAttached = true;
            Check(EsentNative.JetOpenDatabase(sessionId, databasePath, null, out uint databaseId, EsentNative.JetBitDbReadOnly), "唯讀開啟資料庫");

            return new SrumReader(databasePath, workDirectory, instance, sessionId, databaseId);
        }
        catch
        {
            if (databaseAttached)
                TryCleanup("卸離資料庫", () => EsentNative.JetDetachDatabase(sessionId, databasePath));

            if (sessionStarted)
                TryCleanup("結束 ESENT session", () => EsentNative.JetEndSession(sessionId, 0));

            if (instanceCreated)
                TryCleanup("終止 ESENT instance", () => EsentNative.JetTerm(instance));

            DeleteDirectory(workDirectory);
            throw;
        }
    }

    private static void AttachReadOnly(nint sessionId, string databasePath, TimeSpan lockWaitTimeout, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        bool reported = false;

        while (true)
        {
            // 先用一般檔案存取探測，避免每次重試都在事件檢視器留下 ESENT 489 錯誤。
            if (!IsLockedByAnotherProcess(databasePath))
            {
                int result = EsentNative.JetAttachDatabase2(sessionId, databasePath, 0, EsentNative.JetBitDbReadOnly);
                if (result != EsentNative.JetErrFileAccessDenied)
                {
                    Check(result, "唯讀附加資料庫");
                    return;
                }
            }

            if (stopwatch.Elapsed >= lockWaitTimeout)
                throw new EsentException("唯讀附加資料庫（資料庫被其他程序鎖定，可能是 SRU 服務正在寫入）", EsentNative.JetErrFileAccessDenied);

            if (!reported)
            {
                Console.Error.WriteLine($"資料庫正由其他程序使用（通常是 SRU 服務寫入中），最多等待 {lockWaitTimeout.TotalSeconds:0} 秒…");
                reported = true;
            }

            if (cancellationToken.WaitHandle.WaitOne(500))
                cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private static bool IsLockedByAnotherProcess(string databasePath)
    {
        try
        {
            using var probe = new FileStream(databasePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return false;
        }
        catch (IOException ex) when ((ex.HResult & 0xFFFF) is 32 or 33)
        {
            return true;
        }
    }

    public IReadOnlyList<string> GetTableNames()
    {
        var objects = new EsentNative.JetObjectList
        {
            StructSize = (uint)Marshal.SizeOf<EsentNative.JetObjectList>()
        };

        Check(
            EsentNative.JetGetObjectInfo(
                _sessionId,
                _databaseId,
                0,
                null,
                null,
                ref objects,
                objects.StructSize,
                EsentNative.JetObjectInfoList),
            "取得資料表清單");

        var tableNames = new List<string>();
        try
        {
            for (uint i = 0; i < objects.RecordCount; i++)
            {
                if (i == 0)
                    Check(EsentNative.JetMove(_sessionId, objects.TableId, EsentNative.JetMoveFirst, 0), "移至第一個資料表");

                tableNames.Add(ReadText(_sessionId, objects.TableId, objects.ColumnIdObjectName));
                if (i + 1 < objects.RecordCount)
                    Check(EsentNative.JetMove(_sessionId, objects.TableId, EsentNative.JetMoveNext, 0), "移至下一個資料表");
            }
        }
        finally
        {
            TryCleanup("關閉資料表目錄 cursor", () => EsentNative.JetCloseTable(_sessionId, objects.TableId));
        }

        return tableNames;
    }

    public SrumTableSchema GetTable(string tableName)
    {
        nint tableId = 0;
        Check(EsentNative.JetOpenTable(_sessionId, _databaseId, tableName, 0, 0, EsentNative.JetBitTableReadOnly, out tableId), $"唯讀開啟資料表「{tableName}」");

        try
        {
            var columns = new EsentNative.JetColumnList
            {
                StructSize = (uint)Marshal.SizeOf<EsentNative.JetColumnList>()
            };

            Check(
                EsentNative.JetGetTableColumnInfo(
                    _sessionId,
                    tableId,
                    null,
                    ref columns,
                    columns.StructSize,
                    EsentNative.JetColumnInfoList),
                $"取得資料表「{tableName}」的欄位清單");

            var schemaColumns = new List<SrumColumnSchema>();
            try
            {
                for (uint i = 0; i < columns.RecordCount; i++)
                {
                    if (i == 0)
                        Check(EsentNative.JetMove(_sessionId, columns.TableId, EsentNative.JetMoveFirst, 0), "移至第一個欄位");

                    string name = ReadText(_sessionId, columns.TableId, columns.ColumnIdColumnName);
                    uint columnId = ReadUInt32(_sessionId, columns.TableId, columns.ColumnIdColumnId);
                    uint type = ReadUInt32(_sessionId, columns.TableId, columns.ColumnIdColumnType);
                    uint maxLength = ReadUInt32(_sessionId, columns.TableId, columns.ColumnIdMaxLength);
                    uint codePage = ReadUInt16(_sessionId, columns.TableId, columns.ColumnIdCodePage);
                    schemaColumns.Add(new SrumColumnSchema(name, columnId, type, ColumnTypeName(type), maxLength, codePage));

                    if (i + 1 < columns.RecordCount)
                        Check(EsentNative.JetMove(_sessionId, columns.TableId, EsentNative.JetMoveNext, 0), "移至下一個欄位");
                }
            }
            finally
            {
                TryCleanup("關閉欄位目錄 cursor", () => EsentNative.JetCloseTable(_sessionId, columns.TableId));
            }

            var indexes = ReadIndexes(_sessionId, tableId, tableName);
            return new SrumTableSchema(tableName, schemaColumns, indexes);
        }
        finally
        {
            TryCleanup($"關閉資料表「{tableName}」cursor", () => EsentNative.JetCloseTable(_sessionId, tableId));
        }
    }

    public IReadOnlyList<IReadOnlyList<SrumDecodedValue>> ReadTable(string tableName, uint maxRows)
    {
        SrumTableSchema schema = GetTable(tableName);
        nint tableId = 0;
        Check(EsentNative.JetOpenTable(_sessionId, _databaseId, tableName, 0, 0, EsentNative.JetBitTableReadOnly, out tableId), $"唯讀開啟資料表「{tableName}」");

        try
        {
            var rows = new List<IReadOnlyList<SrumDecodedValue>>();
            if (maxRows == 0)
                return rows;

            int moveResult = EsentNative.JetMove(_sessionId, tableId, EsentNative.JetMoveFirst, 0);
            if (moveResult == EsentNative.JetErrNoCurrentRecord)
                return rows;

            Check(moveResult, $"移至資料表「{tableName}」的第一筆資料");

            for (uint rowNumber = 1; rowNumber <= maxRows; rowNumber++)
            {
                int remainingRowBytes = 1024 * 1024;
                var values = new List<SrumDecodedValue>(schema.Columns.Count);
                foreach (SrumColumnSchema column in schema.Columns)
                {
                    SrumRawColumnValue raw = ReadRowColumn(_sessionId, tableId, column, remainingRowBytes);
                    if (raw.Data is not null)
                        remainingRowBytes = Math.Max(0, remainingRowBytes - raw.Data.Length);

                    values.Add(SrumValueDecoder.Decode(column, raw));
                }

                rows.Add(values);
                if (rowNumber == maxRows)
                    break;

                moveResult = EsentNative.JetMove(_sessionId, tableId, EsentNative.JetMoveNext, 0);
                if (moveResult == EsentNative.JetErrNoCurrentRecord)
                    break;

                Check(moveResult, $"移至資料表「{tableName}」的下一筆資料");
            }

            return rows;
        }
        finally
        {
            TryCleanup($"關閉資料表「{tableName}」cursor", () => EsentNative.JetCloseTable(_sessionId, tableId));
        }
    }

    public IReadOnlyList<SrumIdMapEntry> ReadIdMap()
    {
        const string tableName = "SruDbIdMapTable";
        SrumTableSchema schema = GetTable(tableName);
        uint idIndexColumn = FindColumnId(schema, "IdIndex");
        uint idTypeColumn = FindColumnId(schema, "IdType");
        uint idBlobColumn = FindColumnId(schema, "IdBlob");

        var entries = new List<SrumIdMapEntry>();
        ScanTable(tableName, tableId =>
            entries.Add(new SrumIdMapEntry(
                ReadInt32(tableId, idIndexColumn, "IdIndex"),
                ReadByte(tableId, idTypeColumn, "IdType"),
                RetrieveValue(_sessionId, tableId, idBlobColumn, "IdBlob", MaxValueBytes))));

        return entries;
    }

    public IReadOnlyList<SrumProviderRecord> ReadProviderRecords(string tableName, string dataColumnName)
    {
        SrumTableSchema schema = GetTable(tableName);
        uint autoIncIdColumn = FindColumnId(schema, "AutoIncId");
        uint timeStampColumn = FindColumnId(schema, "TimeStamp");
        uint appIdColumn = FindColumnId(schema, "AppId");
        uint userIdColumn = FindColumnId(schema, "UserId");
        uint dataColumn = FindColumnId(schema, dataColumnName);

        var records = new List<SrumProviderRecord>();
        ScanTable(tableName, tableId =>
        {
            byte[] timeStamp = RetrieveValue(_sessionId, tableId, timeStampColumn, "TimeStamp", sizeof(double))
                ?? throw new InvalidDataException($"資料表「{tableName}」的 TimeStamp 為 NULL。");
            if (timeStamp.Length != sizeof(double))
                throw new InvalidDataException($"資料表「{tableName}」的 TimeStamp 長度錯誤：{timeStamp.Length} bytes。");

            double oaDate = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(timeStamp));
            records.Add(new SrumProviderRecord(
                ReadInt32(tableId, autoIncIdColumn, "AutoIncId"),
                oaDate,
                ReadInt32(tableId, appIdColumn, "AppId"),
                ReadInt32(tableId, userIdColumn, "UserId"),
                RetrieveValue(_sessionId, tableId, dataColumn, dataColumnName, MaxValueBytes)));
        });

        return records;
    }

    private void ScanTable(string tableName, Action<nint> visitRow)
    {
        nint tableId = 0;
        Check(EsentNative.JetOpenTable(_sessionId, _databaseId, tableName, 0, 0, EsentNative.JetBitTableReadOnly, out tableId), $"唯讀開啟資料表「{tableName}」");

        try
        {
            int moveResult = EsentNative.JetMove(_sessionId, tableId, EsentNative.JetMoveFirst, 0);
            while (moveResult != EsentNative.JetErrNoCurrentRecord)
            {
                Check(moveResult, $"移至資料表「{tableName}」的資料列");
                visitRow(tableId);
                moveResult = EsentNative.JetMove(_sessionId, tableId, EsentNative.JetMoveNext, 0);
            }
        }
        finally
        {
            TryCleanup($"關閉資料表「{tableName}」cursor", () => EsentNative.JetCloseTable(_sessionId, tableId));
        }
    }

    private static uint FindColumnId(SrumTableSchema schema, string columnName) =>
        schema.Columns.FirstOrDefault(column => string.Equals(column.Name, columnName, StringComparison.OrdinalIgnoreCase))?.ColumnId
        ?? throw new InvalidDataException($"資料表「{schema.Name}」找不到欄位「{columnName}」。");

    private int ReadInt32(nint tableId, uint columnId, string columnName)
    {
        byte[] value = RetrieveValue(_sessionId, tableId, columnId, columnName, sizeof(int))
            ?? throw new InvalidDataException($"欄位「{columnName}」為 NULL。");
        if (value.Length != sizeof(int))
            throw new InvalidDataException($"欄位「{columnName}」長度錯誤：預期 4 bytes，實際 {value.Length} bytes。");

        return BinaryPrimitives.ReadInt32LittleEndian(value);
    }

    private byte ReadByte(nint tableId, uint columnId, string columnName)
    {
        byte[] value = RetrieveValue(_sessionId, tableId, columnId, columnName, sizeof(byte))
            ?? throw new InvalidDataException($"欄位「{columnName}」為 NULL。");
        if (value.Length != sizeof(byte))
            throw new InvalidDataException($"欄位「{columnName}」長度錯誤：預期 1 byte，實際 {value.Length} bytes。");

        return value[0];
    }

    private static unsafe byte[]? RetrieveValue(nint sessionId, nint tableId, uint columnId, string columnName, uint maxBytes)
    {
        const int InitialBufferSize = 256;
        byte* initialBuffer = stackalloc byte[InitialBufferSize];
        int result = EsentNative.JetRetrieveColumn(sessionId, tableId, columnId, (nint)initialBuffer, InitialBufferSize, out uint actualSize, 0, 0);
        if (result == EsentNative.JetWrnColumnNull)
            return null;

        CheckRetrieveResult(result, $"讀取欄位「{columnName}」", allowTruncated: true);
        if (actualSize <= InitialBufferSize)
            return new ReadOnlySpan<byte>(initialBuffer, (int)actualSize).ToArray();

        if (actualSize > maxBytes)
            throw new InvalidDataException($"欄位「{columnName}」過大：{actualSize} bytes。");

        byte[] buffer = new byte[actualSize];
        fixed (byte* pinned = buffer)
        {
            result = EsentNative.JetRetrieveColumn(sessionId, tableId, columnId, (nint)pinned, actualSize, out uint copiedSize, 0, 0);
            if (result == EsentNative.JetWrnColumnNull)
                return null;

            CheckRetrieveResult(result, $"讀取欄位「{columnName}」", allowTruncated: false);
            return copiedSize == actualSize ? buffer : buffer.AsSpan(0, (int)Math.Min(copiedSize, actualSize)).ToArray();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        TryCleanup("關閉資料庫", () => EsentNative.JetCloseDatabase(_sessionId, _databaseId, 0));
        TryCleanup("卸離資料庫", () => EsentNative.JetDetachDatabase(_sessionId, _databasePath));
        TryCleanup("結束 ESENT session", () => EsentNative.JetEndSession(_sessionId, 0));
        TryCleanup("終止 ESENT instance", () => EsentNative.JetTerm(_instance));
        DeleteDirectory(_workDirectory);
    }

    private static void DeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void Check(int errorCode, string operation)
    {
        if (errorCode < 0)
            throw new EsentException(operation, errorCode);
    }

    private static void TryCleanup(string operation, Func<int> cleanup)
    {
        try
        {
            Check(cleanup(), operation);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"警告：{ex.Message}");
        }
    }

    private static string CreateDirectory(string root, string name)
    {
        string path = Path.Combine(root, name);
        Directory.CreateDirectory(path);
        return Path.EndsInDirectorySeparator(path) ? path : path + Path.DirectorySeparatorChar;
    }

    private static void SetPath(ref nint instance, uint parameterId, string path)
    {
        Check(EsentNative.JetSetSystemParameter(ref instance, 0, parameterId, 0, path), $"設定 ESENT 路徑 {path}");
    }

    private static string ReadText(nint sessionId, nint tableId, uint columnId)
    {
        byte[] bytes = RetrieveColumn(sessionId, tableId, columnId);
        bool isUtf16 = bytes.Length >= 2 && bytes.Length % 2 == 0;

        for (int i = 1; isUtf16 && i < bytes.Length; i += 2)
            isUtf16 = bytes[i] == 0;

        Encoding encoding = isUtf16 ? Encoding.Unicode : Encoding.Latin1;
        return encoding.GetString(bytes).TrimEnd('\0');
    }

    private static ushort ReadUInt16(nint sessionId, nint tableId, uint columnId)
    {
        byte[] bytes = RetrieveColumn(sessionId, tableId, columnId);
        if (bytes.Length != sizeof(ushort))
            throw new InvalidOperationException($"ESENT 中繼資料欄位長度錯誤：預期 2 bytes，實際 {bytes.Length} bytes。");

        return BinaryPrimitives.ReadUInt16LittleEndian(bytes);
    }

    private static uint ReadUInt32(nint sessionId, nint tableId, uint columnId)
    {
        byte[] bytes = RetrieveColumn(sessionId, tableId, columnId);
        if (bytes.Length != sizeof(uint))
            throw new InvalidOperationException($"ESENT 中繼資料欄位長度錯誤：預期 4 bytes，實際 {bytes.Length} bytes。");

        return BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    }

    private static byte[] RetrieveColumn(nint sessionId, nint tableId, uint columnId)
    {
        Check(EsentNative.JetRetrieveColumn(sessionId, tableId, columnId, 0, 0, out uint size, 0, 0), "查詢欄位資料長度");

        if (size > 65536)
            throw new InvalidOperationException($"ESENT 中繼資料欄位過大：{size} bytes。");

        if (size == 0)
            return [];

        nint buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            Check(EsentNative.JetRetrieveColumn(sessionId, tableId, columnId, buffer, size, out uint actualSize, 0, 0), "讀取欄位資料");
            if (actualSize > size)
                throw new InvalidOperationException("ESENT 回報的欄位長度超出配置緩衝區。");

            byte[] result = new byte[actualSize];
            Marshal.Copy(buffer, result, 0, (int)actualSize);
            return result;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string ColumnTypeName(uint type) => type switch
    {
        1 => "Bit",
        2 => "UnsignedByte",
        3 => "Short",
        4 => "Long",
        5 => "Currency",
        6 => "IEEESingle",
        7 => "IEEEDouble",
        8 => "DateTime",
        9 => "Binary",
        10 => "Text",
        11 => "LongBinary",
        12 => "LongText",
        13 => "ObsoleteSLV",
        14 => "UnsignedLong",
        15 => "LongLong",
        16 => "GUID",
        17 => "UnsignedShort",
        _ => $"Unknown({type})"
    };

    private static IReadOnlyList<SrumIndexSchema> ReadIndexes(nint sessionId, nint tableId, string tableName)
    {
        var indexList = new EsentNative.JetIndexList
        {
            StructSize = (uint)Marshal.SizeOf<EsentNative.JetIndexList>()
        };

        Check(EsentNative.JetGetTableIndexInfo(sessionId, tableId, null, ref indexList, indexList.StructSize, EsentNative.JetIndexInfoList), $"取得資料表「{tableName}」的索引清單");

        var indexes = new Dictionary<string, (uint Flags, List<SrumIndexKey> Keys)>(StringComparer.OrdinalIgnoreCase);
        try
        {
            for (uint i = 0; i < indexList.RecordCount; i++)
            {
                if (i == 0)
                    Check(EsentNative.JetMove(sessionId, indexList.TableId, EsentNative.JetMoveFirst, 0), "移至第一個索引 key 欄位");

                string indexName = ReadText(sessionId, indexList.TableId, indexList.ColumnIdIndexName);
                string columnName = ReadText(sessionId, indexList.TableId, indexList.ColumnIdColumnName);
                uint columnId = ReadUInt32(sessionId, indexList.TableId, indexList.ColumnIdColumnId);
                uint keyOrder = ReadUInt32(sessionId, indexList.TableId, indexList.ColumnIdKeyColumnIndex);
                uint columnFlags = ReadUInt32(sessionId, indexList.TableId, indexList.ColumnIdColumnGrbit);
                uint indexFlags = ReadUInt32(sessionId, indexList.TableId, indexList.ColumnIdIndexGrbit);

                if (!indexes.TryGetValue(indexName, out var index))
                {
                    index = (indexFlags, []);
                    indexes.Add(indexName, index);
                }

                index.Keys.Add(new SrumIndexKey(indexName, columnName, columnId, keyOrder, (columnFlags & EsentNative.JetBitKeyDescending) != 0));

                if (i + 1 < indexList.RecordCount)
                    Check(EsentNative.JetMove(sessionId, indexList.TableId, EsentNative.JetMoveNext, 0), "移至下一個索引 key 欄位");
            }
        }
        finally
        {
            TryCleanup("關閉索引目錄 cursor", () => EsentNative.JetCloseTable(sessionId, indexList.TableId));
        }

        return indexes
            .Select(pair => new SrumIndexSchema(pair.Key, pair.Value.Flags, pair.Value.Keys.OrderBy(key => key.KeyOrder).ToArray()))
            .OrderBy(index => index.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static SrumRawColumnValue ReadRowColumn(nint sessionId, nint tableId, SrumColumnSchema column, int remainingRowBytes)
    {
        int result = EsentNative.JetRetrieveColumn(sessionId, tableId, column.ColumnId, 0, 0, out uint byteLength, 0, 0);
        if (result == EsentNative.JetWrnColumnNull)
            return new SrumRawColumnValue(true, 0, null, null, null);

        CheckRetrieveResult(result, $"查詢資料列欄位「{column.Name}」長度", allowTruncated: true);
        if (byteLength == 0)
            return new SrumRawColumnValue(false, 0, [], null, null);

        if (!SrumValueDecoder.RequiresFullValue(column))
            return ReadSummary(sessionId, tableId, column.ColumnId, byteLength, "not-decoded-type", $"讀取欄位「{column.Name}」摘要");

        if (byteLength > 256 * 1024 || byteLength > (uint)remainingRowBytes)
            return ReadSummary(sessionId, tableId, column.ColumnId, byteLength, "not-decoded-size-limit", $"讀取欄位「{column.Name}」摘要");

        nint buffer = Marshal.AllocHGlobal((int)byteLength);
        try
        {
            result = EsentNative.JetRetrieveColumn(sessionId, tableId, column.ColumnId, buffer, byteLength, out uint actualSize, 0, 0);
            if (result == EsentNative.JetWrnColumnNull)
                return new SrumRawColumnValue(true, 0, null, null, null);

            CheckRetrieveResult(result, $"讀取資料列欄位「{column.Name}」", allowTruncated: true);
            int copiedSize = (int)Math.Min(actualSize, byteLength);
            byte[] data = new byte[copiedSize];
            if (copiedSize > 0)
                Marshal.Copy(buffer, data, 0, copiedSize);

            if (result == EsentNative.JetWrnBufferTruncated || actualSize > byteLength)
            {
                string? preview = data.Length == 0 ? null : Convert.ToHexString(data.AsSpan(0, Math.Min(data.Length, 16)));
                return new SrumRawColumnValue(false, actualSize, null, "not-decoded-truncated", preview);
            }

            return new SrumRawColumnValue(false, (uint)copiedSize, data, null, null);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static SrumRawColumnValue ReadSummary(nint sessionId, nint tableId, uint columnId, uint byteLength, string status, string operation)
    {
        int previewSize = (int)Math.Min(byteLength, 16);
        nint buffer = Marshal.AllocHGlobal(previewSize);
        try
        {
            int result = EsentNative.JetRetrieveColumn(sessionId, tableId, columnId, buffer, (uint)previewSize, out uint actualSize, 0, 0);
            if (result == EsentNative.JetWrnColumnNull)
                return new SrumRawColumnValue(true, 0, null, null, null);

            CheckRetrieveResult(result, operation, allowTruncated: true);
            int copiedSize = (int)Math.Min(actualSize, (uint)previewSize);
            byte[] preview = new byte[copiedSize];
            if (copiedSize > 0)
                Marshal.Copy(buffer, preview, 0, copiedSize);

            return new SrumRawColumnValue(false, byteLength, null, status, copiedSize == 0 ? null : Convert.ToHexString(preview));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static void CheckRetrieveResult(int errorCode, string operation, bool allowTruncated)
    {
        if (errorCode < 0)
            Check(errorCode, operation);

        if (errorCode == 0 || errorCode == EsentNative.JetWrnColumnNull || allowTruncated && errorCode == EsentNative.JetWrnBufferTruncated)
            return;

        throw new EsentException(operation, errorCode);
    }
}

internal static class SrumSchemaReader
{
    private const uint JetObjectTypeNil = 0;
    private const int MaxMetadataBytes = 65536;
    private const uint MaxColumnValueBytes = 256 * 1024;
    private const int MaxDecodedBytesPerRecord = 1024 * 1024;
    private const int MaxBinaryPreviewBytes = 16;

    public static void Print(string databasePath, uint maxRows, SrumReportWriter output)
    {
        string workDirectory = Path.Combine(Path.GetTempPath(), $"SrumSchemaReader-{Guid.NewGuid():N}");
        nint instance = 0;
        nint sessionId = 0;
        uint databaseId = uint.MaxValue;
        bool instanceCreated = false;
        bool sessionStarted = false;
        bool databaseAttached = false;
        bool databaseOpened = false;

        try
        {
            string systemPath = CreateDirectory(workDirectory, "system");
            string tempPath = CreateDirectory(workDirectory, "temp");
            string logPath = CreateDirectory(workDirectory, "logs");

            Check(EsentNative.JetCreateInstance(out instance, "SrumSchemaReader"), "建立 ESENT instance");
            instanceCreated = true;

            SetPath(ref instance, EsentNative.JetParamSystemPath, systemPath);
            SetPath(ref instance, EsentNative.JetParamTempPath, tempPath);
            SetPath(ref instance, EsentNative.JetParamLogFilePath, logPath);
            Check(EsentNative.JetSetSystemParameter(ref instance, 0, EsentNative.JetParamBaseName, 0, "SRU"), "設定 ESENT 檔名前綴");

            Check(EsentNative.JetInit(ref instance), "初始化 ESENT instance");

            Check(EsentNative.JetBeginSession(instance, out sessionId, null, null), "建立 ESENT session");
            sessionStarted = true;

            Check(EsentNative.JetAttachDatabase2(sessionId, databasePath, 0, EsentNative.JetBitDbReadOnly), "唯讀附加資料庫");
            databaseAttached = true;

            Check(EsentNative.JetOpenDatabase(sessionId, databasePath, null, out databaseId, EsentNative.JetBitDbReadOnly), "唯讀開啟資料庫");
            databaseOpened = true;

            PrintTables(sessionId, databaseId, maxRows, output);
        }
        finally
        {
            if (databaseOpened)
                TryCleanup("關閉資料庫", () => EsentNative.JetCloseDatabase(sessionId, databaseId, 0));

            if (databaseAttached)
                TryCleanup("卸離資料庫", () => EsentNative.JetDetachDatabase(sessionId, databasePath));

            if (sessionStarted)
                TryCleanup("結束 ESENT session", () => EsentNative.JetEndSession(sessionId, 0));

            if (instanceCreated)
                TryCleanup("終止 ESENT instance", () => EsentNative.JetTerm(instance));

            try
            {
                Directory.Delete(workDirectory, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    public static void PrintTable(string databasePath, string tableName, uint maxRows, SrumReportWriter output)
    {
        string workDirectory = Path.Combine(Path.GetTempPath(), $"SrumTableReader-{Guid.NewGuid():N}");
        nint instance = 0;
        nint sessionId = 0;
        uint databaseId = uint.MaxValue;
        bool instanceCreated = false;
        bool sessionStarted = false;
        bool databaseAttached = false;
        bool databaseOpened = false;

        try
        {
            string systemPath = CreateDirectory(workDirectory, "system");
            string tempPath = CreateDirectory(workDirectory, "temp");
            string logPath = CreateDirectory(workDirectory, "logs");

            Check(EsentNative.JetCreateInstance(out instance, "SrumSchemaReader"), "建立 ESENT instance");
            instanceCreated = true;

            SetPath(ref instance, EsentNative.JetParamSystemPath, systemPath);
            SetPath(ref instance, EsentNative.JetParamTempPath, tempPath);
            SetPath(ref instance, EsentNative.JetParamLogFilePath, logPath);
            Check(EsentNative.JetSetSystemParameter(ref instance, 0, EsentNative.JetParamBaseName, 0, "SRU"), "設定 ESENT 檔名前綴");

            Check(EsentNative.JetInit(ref instance), "初始化 ESENT instance");
            Check(EsentNative.JetBeginSession(instance, out sessionId, null, null), "建立 ESENT session");
            sessionStarted = true;

            Check(EsentNative.JetAttachDatabase2(sessionId, databasePath, 0, EsentNative.JetBitDbReadOnly), "唯讀附加資料庫");
            databaseAttached = true;

            Check(EsentNative.JetOpenDatabase(sessionId, databasePath, null, out databaseId, EsentNative.JetBitDbReadOnly), "唯讀開啟資料庫");
            databaseOpened = true;

            PrintColumns(sessionId, databaseId, tableName, maxRows, output);
        }
        finally
        {
            if (databaseOpened)
                TryCleanup("關閉資料庫", () => EsentNative.JetCloseDatabase(sessionId, databaseId, 0));

            if (databaseAttached)
                TryCleanup("卸離資料庫", () => EsentNative.JetDetachDatabase(sessionId, databasePath));

            if (sessionStarted)
                TryCleanup("結束 ESENT session", () => EsentNative.JetEndSession(sessionId, 0));

            if (instanceCreated)
                TryCleanup("終止 ESENT instance", () => EsentNative.JetTerm(instance));

            try
            {
                Directory.Delete(workDirectory, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    public static void PrintSummary(string databasePath, string? tableFilter, SrumReportWriter output)
    {
        string workDirectory = Path.Combine(Path.GetTempPath(), $"SrumTableSummary-{Guid.NewGuid():N}");
        nint instance = 0;
        nint sessionId = 0;
        uint databaseId = uint.MaxValue;
        bool instanceCreated = false;
        bool sessionStarted = false;
        bool databaseAttached = false;
        bool databaseOpened = false;

        try
        {
            string systemPath = CreateDirectory(workDirectory, "system");
            string tempPath = CreateDirectory(workDirectory, "temp");
            string logPath = CreateDirectory(workDirectory, "logs");

            Check(EsentNative.JetCreateInstance(out instance, "SrumSchemaReader"), "建立 ESENT instance");
            instanceCreated = true;

            SetPath(ref instance, EsentNative.JetParamSystemPath, systemPath);
            SetPath(ref instance, EsentNative.JetParamTempPath, tempPath);
            SetPath(ref instance, EsentNative.JetParamLogFilePath, logPath);
            Check(EsentNative.JetSetSystemParameter(ref instance, 0, EsentNative.JetParamBaseName, 0, "SRU"), "設定 ESENT 檔名前綴");

            Check(EsentNative.JetInit(ref instance), "初始化 ESENT instance");

            Check(EsentNative.JetBeginSession(instance, out sessionId, null, null), "建立 ESENT session");
            sessionStarted = true;

            Check(EsentNative.JetAttachDatabase2(sessionId, databasePath, 0, EsentNative.JetBitDbReadOnly), "唯讀附加資料庫");
            databaseAttached = true;

            Check(EsentNative.JetOpenDatabase(sessionId, databasePath, null, out databaseId, EsentNative.JetBitDbReadOnly), "唯讀開啟資料庫");
            databaseOpened = true;

            var tables = GetTableSummaries(sessionId, databaseId, tableFilter);
            output.WriteTableSummary(databasePath, tables);
        }
        finally
        {
            if (databaseOpened)
                TryCleanup("關閉資料庫", () => EsentNative.JetCloseDatabase(sessionId, databaseId, 0));

            if (databaseAttached)
                TryCleanup("卸離資料庫", () => EsentNative.JetDetachDatabase(sessionId, databasePath));

            if (sessionStarted)
                TryCleanup("結束 ESENT session", () => EsentNative.JetEndSession(sessionId, 0));

            if (instanceCreated)
                TryCleanup("終止 ESENT instance", () => EsentNative.JetTerm(instance));

            try
            {
                Directory.Delete(workDirectory, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static IReadOnlyList<SrumTableSummary> GetTableSummaries(nint sessionId, uint databaseId, string? tableFilter)
    {
        var objects = new EsentNative.JetObjectList
        {
            StructSize = (uint)Marshal.SizeOf<EsentNative.JetObjectList>()
        };

        Check(
            EsentNative.JetGetObjectInfo(
                sessionId,
                databaseId,
                JetObjectTypeNil,
                null,
                null,
                ref objects,
                objects.StructSize,
                EsentNative.JetObjectInfoList),
            "取得資料表清單");

        var summaries = new List<SrumTableSummary>();
        try
        {
            for (uint i = 0; i < objects.RecordCount; i++)
            {
                if (i == 0)
                    Check(EsentNative.JetMove(sessionId, objects.TableId, EsentNative.JetMoveFirst, 0), "移至第一個資料表");

                string tableName = ReadText(sessionId, objects.TableId, objects.ColumnIdObjectName);
                if (tableFilter is not null && !string.Equals(tableName, tableFilter, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < objects.RecordCount)
                        Check(EsentNative.JetMove(sessionId, objects.TableId, EsentNative.JetMoveNext, 0), "移至下一個資料表");
                    continue;
                }

                nint tableId = 0;
                Check(EsentNative.JetOpenTable(sessionId, databaseId, tableName, 0, 0, EsentNative.JetBitTableReadOnly, out tableId), $"唯讀開啟資料表「{tableName}」");
                try
                {
                    var columns = new EsentNative.JetColumnList
                    {
                        StructSize = (uint)Marshal.SizeOf<EsentNative.JetColumnList>()
                    };

                    Check(
                        EsentNative.JetGetTableColumnInfo(
                            sessionId,
                            tableId,
                            null,
                            ref columns,
                            columns.StructSize,
                            EsentNative.JetColumnInfoList),
                        $"取得資料表「{tableName}」的欄位清單");

                    var indexList = new EsentNative.JetIndexList
                    {
                        StructSize = (uint)Marshal.SizeOf<EsentNative.JetIndexList>()
                    };

                    Check(
                        EsentNative.JetGetTableIndexInfo(
                            sessionId,
                            tableId,
                            null,
                            ref indexList,
                            indexList.StructSize,
                            EsentNative.JetIndexInfoList),
                        $"取得資料表「{tableName}」的索引清單");

                    summaries.Add(new SrumTableSummary(tableName, (int)columns.RecordCount, (int)indexList.RecordCount, CountRows(sessionId, tableId)));
                }
                finally
                {
                    TryCleanup($"關閉資料表「{tableName}」cursor", () => EsentNative.JetCloseTable(sessionId, tableId));
                }

                if (i + 1 < objects.RecordCount)
                    Check(EsentNative.JetMove(sessionId, objects.TableId, EsentNative.JetMoveNext, 0), "移至下一個資料表");
            }
        }
        finally
        {
            TryCleanup("關閉資料表目錄 cursor", () => EsentNative.JetCloseTable(sessionId, objects.TableId));
        }

        return summaries;
    }

    private static ulong CountRows(nint sessionId, nint tableId)
    {
        int moveResult = EsentNative.JetMove(sessionId, tableId, EsentNative.JetMoveFirst, 0);
        if (moveResult == EsentNative.JetErrNoCurrentRecord)
            return 0;

        Check(moveResult, "移至資料表第一筆資料");

        ulong rowCount = 1;
        while (true)
        {
            moveResult = EsentNative.JetMove(sessionId, tableId, EsentNative.JetMoveNext, 0);
            if (moveResult == EsentNative.JetErrNoCurrentRecord)
                return rowCount;

            Check(moveResult, "移至資料表下一筆資料");
            rowCount++;
        }
    }

    private static void PrintTables(nint sessionId, uint databaseId, uint maxRows, SrumReportWriter output)
    {
        var objects = new EsentNative.JetObjectList
        {
            StructSize = (uint)Marshal.SizeOf<EsentNative.JetObjectList>()
        };

        Check(
            EsentNative.JetGetObjectInfo(
                sessionId,
                databaseId,
                JetObjectTypeNil,
                null,
                null,
                ref objects,
                objects.StructSize,
                EsentNative.JetObjectInfoList),
            "取得資料表清單");

        try
        {
            output.WriteDatabase(databaseId, objects.RecordCount);

            for (uint i = 0; i < objects.RecordCount; i++)
            {
                if (i == 0)
                    Check(EsentNative.JetMove(sessionId, objects.TableId, EsentNative.JetMoveFirst, 0), "移至第一個資料表");

                string tableName = ReadText(sessionId, objects.TableId, objects.ColumnIdObjectName);
                PrintColumns(sessionId, databaseId, tableName, maxRows, output);

                if (i + 1 < objects.RecordCount)
                    Check(EsentNative.JetMove(sessionId, objects.TableId, EsentNative.JetMoveNext, 0), "移至下一個資料表");
            }
        }
        finally
        {
            TryCleanup("關閉資料表目錄 cursor", () => EsentNative.JetCloseTable(sessionId, objects.TableId));
        }
    }

    private static void PrintColumns(nint sessionId, uint databaseId, string tableName, uint maxRows, SrumReportWriter output)
    {
        nint tableId = 0;
        Check(EsentNative.JetOpenTable(sessionId, databaseId, tableName, 0, 0, EsentNative.JetBitTableReadOnly, out tableId), $"唯讀開啟資料表「{tableName}」");

        try
        {
            var columns = new EsentNative.JetColumnList
            {
                StructSize = (uint)Marshal.SizeOf<EsentNative.JetColumnList>()
            };

            Check(
                EsentNative.JetGetTableColumnInfo(
                    sessionId,
                    tableId,
                    null,
                    ref columns,
                    columns.StructSize,
                    EsentNative.JetColumnInfoList),
                $"取得資料表「{tableName}」的欄位清單");

            var schemaColumns = new List<SrumColumnSchema>();
            try
            {
                for (uint i = 0; i < columns.RecordCount; i++)
                {
                    if (i == 0)
                        Check(EsentNative.JetMove(sessionId, columns.TableId, EsentNative.JetMoveFirst, 0), "移至第一個欄位");

                    string name = ReadText(sessionId, columns.TableId, columns.ColumnIdColumnName);
                    uint columnId = ReadUInt32(sessionId, columns.TableId, columns.ColumnIdColumnId);
                    uint type = ReadUInt32(sessionId, columns.TableId, columns.ColumnIdColumnType);
                    uint maxLength = ReadUInt32(sessionId, columns.TableId, columns.ColumnIdMaxLength);
                    uint codePage = ReadUInt16(sessionId, columns.TableId, columns.ColumnIdCodePage);
                    schemaColumns.Add(new SrumColumnSchema(name, columnId, type, ColumnTypeName(type), maxLength, codePage));

                    if (i + 1 < columns.RecordCount)
                        Check(EsentNative.JetMove(sessionId, columns.TableId, EsentNative.JetMoveNext, 0), "移至下一個欄位");
                }
            }
            finally
            {
                TryCleanup("關閉欄位目錄 cursor", () => EsentNative.JetCloseTable(sessionId, columns.TableId));
            }

            IReadOnlyList<SrumIndexSchema> indexes = ReadIndexes(sessionId, tableId, tableName);
            output.WriteTable(tableName, schemaColumns.Count, indexes.Count);

            foreach (SrumColumnSchema column in schemaColumns)
            {
                SrumIndexKey[] indexKeys = indexes
                    .SelectMany(index => index.KeyColumns)
                    .Where(key => key.ColumnId == column.ColumnId)
                    .OrderBy(key => key.IndexName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(key => key.KeyOrder)
                    .ToArray();
                output.WriteColumn(tableName, column, indexKeys);
            }

            foreach (SrumIndexSchema index in indexes)
                output.WriteIndex(tableName, index);

            PrintRows(sessionId, tableId, tableName, schemaColumns, maxRows, output);
        }
        finally
        {
            TryCleanup($"關閉資料表「{tableName}」cursor", () => EsentNative.JetCloseTable(sessionId, tableId));
        }
    }

    private static IReadOnlyList<SrumIndexSchema> ReadIndexes(nint sessionId, nint tableId, string tableName)
    {
        var indexList = new EsentNative.JetIndexList
        {
            StructSize = (uint)Marshal.SizeOf<EsentNative.JetIndexList>()
        };

        Check(
            EsentNative.JetGetTableIndexInfo(
                sessionId,
                tableId,
                null,
                ref indexList,
                indexList.StructSize,
                EsentNative.JetIndexInfoList),
            $"取得資料表「{tableName}」的索引清單");

        var indexes = new Dictionary<string, (uint Flags, List<SrumIndexKey> Keys)>(StringComparer.OrdinalIgnoreCase);
        try
        {
            for (uint i = 0; i < indexList.RecordCount; i++)
            {
                if (i == 0)
                    Check(EsentNative.JetMove(sessionId, indexList.TableId, EsentNative.JetMoveFirst, 0), "移至第一個索引 key 欄位");

                string indexName = ReadText(sessionId, indexList.TableId, indexList.ColumnIdIndexName);
                string columnName = ReadText(sessionId, indexList.TableId, indexList.ColumnIdColumnName);
                uint columnId = ReadUInt32(sessionId, indexList.TableId, indexList.ColumnIdColumnId);
                uint keyOrder = ReadUInt32(sessionId, indexList.TableId, indexList.ColumnIdKeyColumnIndex);
                uint columnFlags = ReadUInt32(sessionId, indexList.TableId, indexList.ColumnIdColumnGrbit);
                uint indexFlags = ReadUInt32(sessionId, indexList.TableId, indexList.ColumnIdIndexGrbit);

                if (!indexes.TryGetValue(indexName, out var index))
                {
                    index = (indexFlags, []);
                    indexes.Add(indexName, index);
                }

                index.Keys.Add(new SrumIndexKey(
                    indexName,
                    columnName,
                    columnId,
                    keyOrder,
                    (columnFlags & EsentNative.JetBitKeyDescending) != 0));

                if (i + 1 < indexList.RecordCount)
                    Check(EsentNative.JetMove(sessionId, indexList.TableId, EsentNative.JetMoveNext, 0), "移至下一個索引 key 欄位");
            }
        }
        finally
        {
            TryCleanup("關閉索引目錄 cursor", () => EsentNative.JetCloseTable(sessionId, indexList.TableId));
        }

        return indexes
            .Select(pair => new SrumIndexSchema(
                pair.Key,
                pair.Value.Flags,
                pair.Value.Keys.OrderBy(key => key.KeyOrder).ToArray()))
            .OrderBy(index => index.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void PrintRows(
        nint sessionId,
        nint tableId,
        string tableName,
        IReadOnlyList<SrumColumnSchema> columns,
        uint maxRows,
        SrumReportWriter output)
    {
        if (maxRows == 0 || output.IsTruncated)
            return;

        int moveResult = EsentNative.JetMove(sessionId, tableId, EsentNative.JetMoveFirst, 0);
        if (moveResult == EsentNative.JetErrNoCurrentRecord)
            return;

        Check(moveResult, $"移至資料表「{tableName}」的第一筆資料");

        for (uint rowNumber = 1; rowNumber <= maxRows && !output.IsTruncated; rowNumber++)
        {
            int remainingRowBytes = MaxDecodedBytesPerRecord;
            var values = new List<SrumDecodedValue>(columns.Count);
            foreach (SrumColumnSchema column in columns)
            {
                SrumRawColumnValue raw = ReadRowColumn(sessionId, tableId, column, remainingRowBytes);
                if (raw.Data is not null)
                    remainingRowBytes = Math.Max(0, remainingRowBytes - raw.Data.Length);

                values.Add(SrumValueDecoder.Decode(column, raw));
            }

            output.WriteRecord(tableName, rowNumber, values);
            if (rowNumber == maxRows || output.IsTruncated)
                break;

            moveResult = EsentNative.JetMove(sessionId, tableId, EsentNative.JetMoveNext, 0);
            if (moveResult == EsentNative.JetErrNoCurrentRecord)
                break;

            Check(moveResult, $"移至資料表「{tableName}」的下一筆資料");
        }
    }

    private static SrumRawColumnValue ReadRowColumn(nint sessionId, nint tableId, SrumColumnSchema column, int remainingRowBytes)
    {
        int result = EsentNative.JetRetrieveColumn(sessionId, tableId, column.ColumnId, 0, 0, out uint byteLength, 0, 0);
        if (result == EsentNative.JetWrnColumnNull)
            return new SrumRawColumnValue(true, 0, null, null, null);

        CheckRetrieveResult(result, $"查詢資料列欄位「{column.Name}」長度", allowTruncated: true);
        if (byteLength == 0)
            return new SrumRawColumnValue(false, 0, [], null, null);

        if (!SrumValueDecoder.RequiresFullValue(column))
            return ReadSummary(sessionId, tableId, column.ColumnId, byteLength, "not-decoded-type", $"讀取欄位「{column.Name}」摘要");

        if (byteLength > MaxColumnValueBytes || byteLength > (uint)remainingRowBytes)
            return ReadSummary(sessionId, tableId, column.ColumnId, byteLength, "not-decoded-size-limit", $"讀取欄位「{column.Name}」摘要");

        nint buffer = Marshal.AllocHGlobal((int)byteLength);
        try
        {
            result = EsentNative.JetRetrieveColumn(sessionId, tableId, column.ColumnId, buffer, byteLength, out uint actualSize, 0, 0);
            if (result == EsentNative.JetWrnColumnNull)
                return new SrumRawColumnValue(true, 0, null, null, null);

            CheckRetrieveResult(result, $"讀取資料列欄位「{column.Name}」", allowTruncated: true);
            int copiedSize = (int)Math.Min(actualSize, byteLength);
            byte[] data = new byte[copiedSize];
            if (copiedSize > 0)
                Marshal.Copy(buffer, data, 0, copiedSize);

            if (result == EsentNative.JetWrnBufferTruncated || actualSize > byteLength)
            {
                string? preview = data.Length == 0 ? null : Convert.ToHexString(data.AsSpan(0, Math.Min(data.Length, MaxBinaryPreviewBytes)));
                return new SrumRawColumnValue(false, actualSize, null, "not-decoded-truncated", preview);
            }

            return new SrumRawColumnValue(false, (uint)copiedSize, data, null, null);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static SrumRawColumnValue ReadSummary(
        nint sessionId,
        nint tableId,
        uint columnId,
        uint byteLength,
        string status,
        string operation)
    {
        int previewSize = (int)Math.Min(byteLength, MaxBinaryPreviewBytes);
        nint buffer = Marshal.AllocHGlobal(previewSize);
        try
        {
            int result = EsentNative.JetRetrieveColumn(sessionId, tableId, columnId, buffer, (uint)previewSize, out uint actualSize, 0, 0);
            if (result == EsentNative.JetWrnColumnNull)
                return new SrumRawColumnValue(true, 0, null, null, null);

            CheckRetrieveResult(result, operation, allowTruncated: true);
            int copiedSize = (int)Math.Min(actualSize, (uint)previewSize);
            byte[] preview = new byte[copiedSize];
            if (copiedSize > 0)
                Marshal.Copy(buffer, preview, 0, copiedSize);

            return new SrumRawColumnValue(false, byteLength, null, status, copiedSize == 0 ? null : Convert.ToHexString(preview));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static void CheckRetrieveResult(int errorCode, string operation, bool allowTruncated)
    {
        if (errorCode < 0)
            Check(errorCode, operation);

        if (errorCode == 0 || errorCode == EsentNative.JetWrnColumnNull || allowTruncated && errorCode == EsentNative.JetWrnBufferTruncated)
            return;

        throw new EsentException(operation, errorCode);
    }

    private static string CreateDirectory(string root, string name)
    {
        string path = Path.Combine(root, name);
        Directory.CreateDirectory(path);
        return Path.EndsInDirectorySeparator(path) ? path : path + Path.DirectorySeparatorChar;
    }

    private static void SetPath(ref nint instance, uint parameterId, string path)
    {
        Check(EsentNative.JetSetSystemParameter(ref instance, 0, parameterId, 0, path), $"設定 ESENT 路徑 {path}");
    }

    private static string ReadText(nint sessionId, nint tableId, uint columnId)
    {
        byte[] bytes = RetrieveColumn(sessionId, tableId, columnId);
        bool isUtf16 = bytes.Length >= 2 && bytes.Length % 2 == 0;

        for (int i = 1; isUtf16 && i < bytes.Length; i += 2)
            isUtf16 = bytes[i] == 0;

        Encoding encoding = isUtf16 ? Encoding.Unicode : Encoding.Latin1;
        return encoding.GetString(bytes).TrimEnd('\0');
    }

    private static ushort ReadUInt16(nint sessionId, nint tableId, uint columnId)
    {
        byte[] bytes = RetrieveColumn(sessionId, tableId, columnId);
        if (bytes.Length != sizeof(ushort))
            throw new InvalidOperationException($"ESENT 中繼資料欄位長度錯誤：預期 2 bytes，實際 {bytes.Length} bytes。");

        return BinaryPrimitives.ReadUInt16LittleEndian(bytes);
    }

    private static uint ReadUInt32(nint sessionId, nint tableId, uint columnId)
    {
        byte[] bytes = RetrieveColumn(sessionId, tableId, columnId);
        if (bytes.Length != sizeof(uint))
            throw new InvalidOperationException($"ESENT 中繼資料欄位長度錯誤：預期 4 bytes，實際 {bytes.Length} bytes。");

        return BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    }

    private static byte[] RetrieveColumn(nint sessionId, nint tableId, uint columnId)
    {
        Check(EsentNative.JetRetrieveColumn(sessionId, tableId, columnId, 0, 0, out uint size, 0, 0), "查詢欄位資料長度");

        if (size > MaxMetadataBytes)
            throw new InvalidOperationException($"ESENT 中繼資料欄位過大：{size} bytes。");

        if (size == 0)
            return [];

        nint buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            Check(EsentNative.JetRetrieveColumn(sessionId, tableId, columnId, buffer, size, out uint actualSize, 0, 0), "讀取欄位資料");
            if (actualSize > size)
                throw new InvalidOperationException("ESENT 回報的欄位長度超出配置緩衝區。");

            byte[] result = new byte[actualSize];
            Marshal.Copy(buffer, result, 0, (int)actualSize);
            return result;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string ColumnTypeName(uint type) => type switch
    {
        1 => "Bit",
        2 => "UnsignedByte",
        3 => "Short",
        4 => "Long",
        5 => "Currency",
        6 => "IEEESingle",
        7 => "IEEEDouble",
        8 => "DateTime",
        9 => "Binary",
        10 => "Text",
        11 => "LongBinary",
        12 => "LongText",
        13 => "ObsoleteSLV",
        14 => "UnsignedLong",
        15 => "LongLong",
        16 => "GUID",
        17 => "UnsignedShort",
        _ => $"Unknown({type})"
    };

    private static void Check(int errorCode, string operation)
    {
        if (errorCode < 0)
            throw new EsentException(operation, errorCode);
    }

    private static void TryCleanup(string operation, Func<int> cleanup)
    {
        try
        {
            Check(cleanup(), operation);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"警告：{ex.Message}");
        }
    }
}

