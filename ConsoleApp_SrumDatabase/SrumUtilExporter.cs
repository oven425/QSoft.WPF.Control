using System.Globalization;
using System.Text;

internal sealed record SrumUtilExportResult(string OutputPath, int RecordCount, int SkippedCount);

// 以 SrumReader 讀出能源預估資料表與 SruDbIdMapTable，輸出成與 powercfg /srumutil 相同的 CSV 或 XML。
internal static class SrumUtilExporter
{
    private const int MaxRowWarnings = 10;

    public static SrumUtilExportResult Export(string databasePath, string outputPath, SrumOutputFormat format, TimeSpan lockWaitTimeout, CancellationToken cancellationToken = default)
    {
        if (format is not (SrumOutputFormat.Csv or SrumOutputFormat.Xml))
            throw new ArgumentOutOfRangeException(nameof(format), format, "匯出格式只能是 CSV 或 XML。");

        string fullOutputPath = Path.GetFullPath(outputPath);
        if (string.Equals(fullOutputPath, Path.GetFullPath(databasePath), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("輸出檔不可與資料庫檔案相同。");

        IReadOnlyList<SrumIdMapEntry> idMapEntries;
        IReadOnlyList<SrumProviderRecord> providerRecords;

        // 資料讀進記憶體後就立刻放開資料庫，讓 SRU 服務可以繼續使用。
        using (SrumReader reader = SrumReader.Open(databasePath, lockWaitTimeout, cancellationToken))
        {
            if (!reader.GetTableNames().Contains(SrumEnergyDecoder.TableName, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"資料庫沒有能源預估資料表 {SrumEnergyDecoder.TableName}。");

            idMapEntries = reader.ReadIdMap();
            providerRecords = reader.ReadProviderRecords(SrumEnergyDecoder.TableName, SrumEnergyDecoder.DataColumnName);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var idMap = new SrumIdMap(idMapEntries);
        var records = new List<SrumEnergyRecord>(providerRecords.Count);
        int skipped = 0;
        int unresolvedIds = 0;

        foreach (SrumProviderRecord source in providerRecords.OrderBy(record => record.AutoIncId))
        {
            try
            {
                if (source.Data is null)
                    throw new InvalidDataException("BinaryData 為 NULL。");

                if (!idMap.TryGetName(source.AppId, out string? appId))
                {
                    appId = string.Empty;
                    unresolvedIds++;
                }

                if (!idMap.TryGetName(source.UserId, out string? userId))
                {
                    userId = string.Empty;
                    unresolvedIds++;
                }

                records.Add(SrumEnergyDecoder.Decode(appId, userId, ToUtc(source.TimeStampOaDate), source.Data));
            }
            catch (InvalidDataException ex)
            {
                skipped++;
                if (skipped <= MaxRowWarnings)
                    Console.Error.WriteLine($"警告：略過 AutoIncId {source.AutoIncId}：{ex.Message}");
            }
        }

        if (skipped > MaxRowWarnings)
            Console.Error.WriteLine($"警告：另外還有 {skipped - MaxRowWarnings} 筆資料列因內容異常被略過。");

        if (unresolvedIds > 0)
            Console.Error.WriteLine($"警告：有 {unresolvedIds} 個 AppId/UserId 在 SruDbIdMapTable 找不到對應，已輸出為空字串。");

        using (var stream = new FileStream(fullOutputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 64 * 1024))
        {
            if (format == SrumOutputFormat.Csv)
                SrumUtilWriter.WriteCsv(writer, records);
            else
                SrumUtilWriter.WriteXml(writer, records);
        }

        return new SrumUtilExportResult(fullOutputPath, records.Count, skipped);
    }

    private static DateTime ToUtc(double oaDate)
    {
        try
        {
            return DateTime.SpecifyKind(DateTime.FromOADate(oaDate), DateTimeKind.Utc);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidDataException($"TimeStamp 不是有效的 OLE 日期：{oaDate.ToString(CultureInfo.InvariantCulture)}。", ex);
        }
    }
}
