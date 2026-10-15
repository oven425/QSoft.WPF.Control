using System.Runtime.InteropServices;

internal static partial class EsentNative
{
    internal const uint JetBitDbReadOnly = 0x00000001;
    internal const uint JetBitTableReadOnly = 0x00000004;
    internal const uint JetObjectInfoList = 1;
    internal const uint JetColumnInfoList = 1;
    internal const uint JetIndexInfoList = 1;
    internal const uint JetBitKeyDescending = 0x00000001;
    internal const int JetWrnColumnNull = 1004;
    internal const int JetWrnBufferTruncated = 1006;
    internal const int JetErrNoCurrentRecord = -1603;
    internal const int JetMoveFirst = int.MinValue;
    internal const int JetMoveNext = 1;
    internal const uint JetParamSystemPath = 0;
    internal const uint JetParamTempPath = 1;
    internal const uint JetParamLogFilePath = 2;
    internal const uint JetParamBaseName = 3;

    [StructLayout(LayoutKind.Sequential)]
    internal struct JetObjectList
    {
        public uint StructSize;
        public nint TableId;
        public uint RecordCount;
        public uint ColumnIdContainerName;
        public uint ColumnIdObjectName;
        public uint ColumnIdObjectType;
        public uint ColumnIdCreateTime;
        public uint ColumnIdUpdateTime;
        public uint ColumnIdGrbit;
        public uint ColumnIdFlags;
        public uint ColumnIdRecordCount;
        public uint ColumnIdPageCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct JetColumnList
    {
        public uint StructSize;
        public nint TableId;
        public uint RecordCount;
        public uint ColumnIdPresentationOrder;
        public uint ColumnIdColumnName;
        public uint ColumnIdColumnId;
        public uint ColumnIdColumnType;
        public uint ColumnIdCountry;
        public uint ColumnIdLanguageId;
        public uint ColumnIdCodePage;
        public uint ColumnIdCollation;
        public uint ColumnIdMaxLength;
        public uint ColumnIdGrbit;
        public uint ColumnIdDefault;
        public uint ColumnIdBaseTableName;
        public uint ColumnIdBaseColumnName;
        public uint ColumnIdDefinitionName;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct JetIndexList
    {
        public uint StructSize;
        public nint TableId;
        public uint RecordCount;
        public uint ColumnIdIndexName;
        public uint ColumnIdIndexGrbit;
        public uint ColumnIdKeyCount;
        public uint ColumnIdEntryCount;
        public uint ColumnIdPageCount;
        public uint ColumnIdColumnCount;
        public uint ColumnIdKeyColumnIndex;
        public uint ColumnIdColumnId;
        public uint ColumnIdColumnType;
        public uint ColumnIdCountry;
        public uint ColumnIdLanguageId;
        public uint ColumnIdCodePage;
        public uint ColumnIdCollation;
        public uint ColumnIdColumnGrbit;
        public uint ColumnIdColumnName;
        public uint ColumnIdLocaleMapFlags;
    }

    [LibraryImport("esent.dll", EntryPoint = "JetCreateInstanceW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int JetCreateInstance(out nint instance, string instanceName);

    [LibraryImport("esent.dll", EntryPoint = "JetSetSystemParameterW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int JetSetSystemParameter(ref nint instance, nint sessionId, uint parameterId, nint parameterValue, string? parameterString);

    [LibraryImport("esent.dll", EntryPoint = "JetInit")]
    internal static partial int JetInit(ref nint instance);

    [LibraryImport("esent.dll", EntryPoint = "JetBeginSessionW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int JetBeginSession(nint instance, out nint sessionId, string? userName, string? password);

    [LibraryImport("esent.dll", EntryPoint = "JetAttachDatabase2W", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int JetAttachDatabase2(nint sessionId, string fileName, uint maxDatabaseSizePages, uint grbit);

    [LibraryImport("esent.dll", EntryPoint = "JetOpenDatabaseW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int JetOpenDatabase(nint sessionId, string fileName, string? connect, out uint databaseId, uint grbit);

    [LibraryImport("esent.dll", EntryPoint = "JetOpenTableW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int JetOpenTable(nint sessionId, uint databaseId, string tableName, nint parameters, uint parameterSize, uint grbit, out nint tableId);

    [LibraryImport("esent.dll", EntryPoint = "JetGetObjectInfoW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int JetGetObjectInfo(nint sessionId, uint databaseId, uint objectType, string? containerName, string? objectName, ref JetObjectList result, uint resultSize, uint infoLevel);

    [LibraryImport("esent.dll", EntryPoint = "JetGetTableColumnInfoW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int JetGetTableColumnInfo(nint sessionId, nint tableId, string? columnName, ref JetColumnList result, uint resultSize, uint infoLevel);

    [LibraryImport("esent.dll", EntryPoint = "JetGetTableIndexInfoW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int JetGetTableIndexInfo(nint sessionId, nint tableId, string? indexName, ref JetIndexList result, uint resultSize, uint infoLevel);

    [LibraryImport("esent.dll", EntryPoint = "JetMove")]
    internal static partial int JetMove(nint sessionId, nint tableId, int rowCount, uint grbit);

    [LibraryImport("esent.dll", EntryPoint = "JetRetrieveColumn")]
    internal static partial int JetRetrieveColumn(nint sessionId, nint tableId, uint columnId, nint data, uint dataSize, out uint actualSize, uint grbit, nint retrieveInfo);

    [LibraryImport("esent.dll", EntryPoint = "JetCloseTable")]
    internal static partial int JetCloseTable(nint sessionId, nint tableId);

    [LibraryImport("esent.dll", EntryPoint = "JetCloseDatabase")]
    internal static partial int JetCloseDatabase(nint sessionId, uint databaseId, uint grbit);

    [LibraryImport("esent.dll", EntryPoint = "JetDetachDatabaseW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int JetDetachDatabase(nint sessionId, string fileName);

    [LibraryImport("esent.dll", EntryPoint = "JetEndSession")]
    internal static partial int JetEndSession(nint sessionId, uint grbit);

    [LibraryImport("esent.dll", EntryPoint = "JetTerm")]
    internal static partial int JetTerm(nint instance);
}
