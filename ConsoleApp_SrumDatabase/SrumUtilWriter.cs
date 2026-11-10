using System.Globalization;

// 輸出格式與 powercfg /srumutil 逐位元組一致：CSV 以 ", " 分隔且不加引號；XML 以兩個空白縮排；換行一律是 CRLF。
internal static class SrumUtilWriter
{
    private const string NewLine = "\r\n";

    private static readonly string[] AttributeNames =
    [
        "AppId", "UserId", "TimeStamp", "OnBattery", "ScreenOn", "BatterySaverActive", "LowPowerEpochActive",
        "Foreground", "InteractivityState", "Container", "Committed", "TimeInMSec", "MeasuredBitmap"
    ];

    private static readonly string[] EnergyNames =
    [
        "EnergyLoss", "CPUEnergyConsumption", "SocEnergyConsumption", "DisplayEnergyConsumption", "DiskEnergyConsumption",
        "NetworkEnergyConsumption", "MBBEnergyConsumption", "NPUEnergyConsumption", "OtherEnergyConsumption", "EmiEnergyConsumption",
        "CPUEnergyConsumptionWorkOnBehalf", "CPUEnergyConsumptionAttributed", "TotalEnergyConsumption"
    ];

    public static void WriteCsv(TextWriter writer, IEnumerable<SrumEnergyRecord> records)
    {
        writer.Write(string.Join(", ", AttributeNames.Concat(EnergyNames)));
        writer.Write(NewLine);

        var attributes = new string[AttributeNames.Length];
        var energies = new ulong[EnergyNames.Length];
        foreach (SrumEnergyRecord record in records)
        {
            FillAttributes(record, attributes);
            FillEnergies(record, energies);

            writer.Write(string.Join(", ", attributes));
            foreach (ulong energy in energies)
            {
                writer.Write(", ");
                writer.Write(energy.ToString(CultureInfo.InvariantCulture));
            }

            writer.Write(NewLine);
        }
    }

    public static void WriteXml(TextWriter writer, IEnumerable<SrumEnergyRecord> records)
    {
        writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        writer.Write(NewLine);

        var attributes = new string[AttributeNames.Length];
        var energies = new ulong[EnergyNames.Length];
        bool hasRecords = false;
        foreach (SrumEnergyRecord record in records)
        {
            if (!hasRecords)
            {
                writer.Write("<RecordSetRows>");
                writer.Write(NewLine);
                hasRecords = true;
            }

            FillAttributes(record, attributes);
            FillEnergies(record, energies);

            writer.Write("  <Record");
            for (int i = 0; i < attributes.Length; i++)
            {
                writer.Write(' ');
                writer.Write(AttributeNames[i]);
                writer.Write("=\"");
                WriteEscapedAttribute(writer, attributes[i]);
                writer.Write('"');
            }

            writer.Write('>');
            writer.Write(NewLine);

            for (int i = 0; i < energies.Length; i++)
            {
                writer.Write("    <");
                writer.Write(EnergyNames[i]);
                writer.Write(" Value=\"");
                writer.Write(energies[i].ToString(CultureInfo.InvariantCulture));
                writer.Write("\" />");
                writer.Write(NewLine);
            }

            writer.Write("  </Record>");
            writer.Write(NewLine);
        }

        // powercfg 的 XML 最後一行沒有換行。
        writer.Write(hasRecords ? "</RecordSetRows>" : "<RecordSetRows />");
    }

    private static void FillAttributes(SrumEnergyRecord record, string[] values)
    {
        values[0] = record.AppId;
        values[1] = record.UserId;
        values[2] = FormatTimeStamp(record.TimeStamp);
        values[3] = FormatBoolean(record.OnBattery);
        values[4] = FormatBoolean(record.ScreenOn);
        values[5] = FormatBoolean(record.BatterySaverActive);
        values[6] = FormatBoolean(record.LowPowerEpochActive);
        values[7] = FormatBoolean(record.Foreground);
        values[8] = FormatInteractivityState(record.InteractivityState);
        values[9] = FormatBoolean(record.Container);
        values[10] = FormatBoolean(record.Committed);
        values[11] = record.TimeInMSec.ToString(CultureInfo.InvariantCulture);
        values[12] = FormatMeasuredBitmap(record.MeasuredMask);
    }

    private static void FillEnergies(SrumEnergyRecord record, ulong[] values)
    {
        values[0] = record.EnergyLoss;
        values[1] = record.CpuEnergy;
        values[2] = record.SocEnergy;
        values[3] = record.DisplayEnergy;
        values[4] = record.DiskEnergy;
        values[5] = record.NetworkEnergy;
        values[6] = record.MbbEnergy;
        values[7] = record.NpuEnergy;
        values[8] = record.OtherEnergy;
        values[9] = record.EmiEnergy;
        values[10] = record.CpuWorkOnBehalfEnergy;
        values[11] = record.CpuAttributedEnergy;
        values[12] = record.TotalEnergy;
    }

    private static string FormatBoolean(bool value) => value ? "TRUE" : "FALSE";

    private static string FormatInteractivityState(SrumInteractivityState state) => state switch
    {
        SrumInteractivityState.NotUnique => "NotUnique",
        SrumInteractivityState.Minimized => "Minimized",
        SrumInteractivityState.Visible => "Visible",
        _ => "Focus"
    };

    // powercfg 以 SYSTEMTIME 格式化：毫秒固定補成 4 位數。
    private static string FormatTimeStamp(DateTime value) =>
        string.Create(CultureInfo.InvariantCulture, $"{value:yyyy-MM-dd}T{value:HH:mm:ss}.{value.Millisecond:D4}");

    // 'b' 加上由低到高排列的 bit 2..10 與 bit 12（bit 11 沒有對應的能源欄位）。
    private static string FormatMeasuredBitmap(ushort mask) =>
        string.Create(11, mask, static (span, bits) =>
        {
            span[0] = 'b';
            int position = 1;
            for (int bit = 2; bit <= 10; bit++)
                span[position++] = (bits & (1 << bit)) != 0 ? '1' : '0';

            span[position] = (bits & (1 << 12)) != 0 ? '1' : '0';
        });

    private static void WriteEscapedAttribute(TextWriter writer, string value)
    {
        foreach (char c in value)
        {
            switch (c)
            {
                case '&':
                    writer.Write("&amp;");
                    break;
                case '<':
                    writer.Write("&lt;");
                    break;
                case '>':
                    writer.Write("&gt;");
                    break;
                case '"':
                    writer.Write("&quot;");
                    break;
                case '\t':
                    writer.Write("&#x9;");
                    break;
                case '\n':
                    writer.Write("&#xA;");
                    break;
                case '\r':
                    writer.Write("&#xD;");
                    break;
                case < ' ' or '\uFFFE' or '\uFFFF':
                    writer.Write('\uFFFD');
                    break;
                default:
                    writer.Write(c);
                    break;
            }
        }
    }
}
