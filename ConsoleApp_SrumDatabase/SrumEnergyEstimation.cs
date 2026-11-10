using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

internal enum SrumInteractivityState : byte
{
    NotUnique = 0,
    Minimized = 1,
    Visible = 2,
    Focus = 3
}

internal sealed record SrumEnergyRecord(
    string AppId,
    string UserId,
    DateTime TimeStamp,
    bool OnBattery,
    bool ScreenOn,
    bool BatterySaverActive,
    bool LowPowerEpochActive,
    bool Foreground,
    SrumInteractivityState InteractivityState,
    bool Container,
    bool Committed,
    uint TimeInMSec,
    ushort MeasuredMask,
    ulong EnergyLoss,
    ulong CpuEnergy,
    ulong SocEnergy,
    ulong DisplayEnergy,
    ulong DiskEnergy,
    ulong NetworkEnergy,
    ulong MbbEnergy,
    ulong NpuEnergy,
    ulong OtherEnergy,
    ulong EmiEnergy,
    ulong CpuWorkOnBehalfEnergy,
    ulong CpuAttributedEnergy)
{
    // powercfg 的 TotalEnergyConsumption 是第一組遮罩內所有數值（含 EnergyLoss）的總和，不含 WorkOnBehalf / Attributed。
    public ulong TotalEnergy => unchecked(
        EnergyLoss + CpuEnergy + SocEnergy + DisplayEnergy + DiskEnergy + NetworkEnergy + MbbEnergy + NpuEnergy + OtherEnergy + EmiEnergy);
}

// 能源預估提供者 {DA73FB89-2BEA-4DDC-86B8-6E048C6DA477} 的 BinaryData 格式。
// 格式沒有公開文件，是由 eeutil.dll（powercfg /srumutil 的實作）的剖析邏輯，加上與 powercfg 實際傾印結果逐筆比對得來：
//   +0  byte    bit0 OnBattery、bit1 ScreenOn、bit2 BatterySaverActive、bit3 LowPowerEpochActive、bit4 Foreground、
//               bit5-6 InteractivityState、bit7 Container
//   +1  byte    bit3：數值區之前多 8 bytes 的版本資訊（powercfg 不輸出）；bit7：Committed
//   +4  uint32  TimeInMSec
//   +8  uint16  遮罩：bit2..10 依序為 EnergyLoss、CPU、Soc、Display、Disk、Network、MBB、NPU、Other，bit12 為 Emi
//   +10 uint16  WorkOnBehalf 區塊遮罩：bit2..10 各有一個值，只有 bit3 是 CPU WorkOnBehalf
//   +12 uint16  Attributed 區塊遮罩：格式同上，只有 bit3 是 CPU Attributed
//   +14 uint16  MeasuredBitmap 來源（輸出時取 bit2..10 與 bit12）
//   +16（有版本資訊時為 +24）起：依序放置第一、二、三組遮罩中已設定位元所對應的 8 bytes 數值
internal static class SrumEnergyDecoder
{
    public const string TableName = "{DA73FB89-2BEA-4DDC-86B8-6E048C6DA477}";
    public const string DataColumnName = "BinaryData";

    private const int HeaderSize = 16;
    private const int VersionFieldSize = 8;
    private const int LossBit = 2;
    private const int CpuBit = 3;
    private const int SocBit = 4;
    private const int DisplayBit = 5;
    private const int DiskBit = 6;
    private const int NetworkBit = 7;
    private const int MbbBit = 8;
    private const int NpuBit = 9;
    private const int OtherBit = 10;
    private const int UnusedBit = 11;
    private const int EmiBit = 12;

    public static SrumEnergyRecord Decode(string appId, string userId, DateTime timeStamp, ReadOnlySpan<byte> blob)
    {
        if (blob.Length < HeaderSize)
            throw new InvalidDataException($"BinaryData 長度不足：{blob.Length} bytes。");

        byte state = blob[0];
        byte flags = blob[1];
        uint timeInMsec = BinaryPrimitives.ReadUInt32LittleEndian(blob[4..]);
        ushort energyMask = BinaryPrimitives.ReadUInt16LittleEndian(blob[8..]);
        ushort workOnBehalfMask = BinaryPrimitives.ReadUInt16LittleEndian(blob[10..]);
        ushort attributedMask = BinaryPrimitives.ReadUInt16LittleEndian(blob[12..]);
        ushort measuredMask = BinaryPrimitives.ReadUInt16LittleEndian(blob[14..]);

        int offset = HeaderSize + ((flags & 0x08) != 0 ? VersionFieldSize : 0);
        Span<ulong> energy = stackalloc ulong[EmiBit + 1];
        for (int bit = LossBit; bit <= EmiBit; bit++)
        {
            if (bit != UnusedBit && (energyMask & (1 << bit)) != 0)
                energy[bit] = ReadValue(blob, ref offset);
        }

        ulong workOnBehalf = ReadCpuValue(blob, ref offset, workOnBehalfMask);
        ulong attributed = ReadCpuValue(blob, ref offset, attributedMask);

        return new SrumEnergyRecord(
            AppId: appId,
            UserId: userId,
            TimeStamp: timeStamp,
            OnBattery: (state & 0x01) != 0,
            ScreenOn: (state & 0x02) != 0,
            BatterySaverActive: (state & 0x04) != 0,
            LowPowerEpochActive: (state & 0x08) != 0,
            Foreground: (state & 0x10) != 0,
            InteractivityState: (SrumInteractivityState)((state >> 5) & 0x03),
            Container: (state & 0x80) != 0,
            Committed: (flags & 0x80) != 0,
            TimeInMSec: timeInMsec,
            MeasuredMask: measuredMask,
            EnergyLoss: energy[LossBit],
            CpuEnergy: energy[CpuBit],
            SocEnergy: energy[SocBit],
            DisplayEnergy: energy[DisplayBit],
            DiskEnergy: energy[DiskBit],
            NetworkEnergy: energy[NetworkBit],
            MbbEnergy: energy[MbbBit],
            NpuEnergy: energy[NpuBit],
            OtherEnergy: energy[OtherBit],
            EmiEnergy: energy[EmiBit],
            CpuWorkOnBehalfEnergy: workOnBehalf,
            CpuAttributedEnergy: attributed);
    }

    // 區塊內每個已設定的位元都佔一個值，必須全部讀過才能對齊下一個區塊；但只有 CPU 那一個會輸出。
    private static ulong ReadCpuValue(ReadOnlySpan<byte> blob, ref int offset, ushort mask)
    {
        ulong cpuValue = 0;
        for (int bit = LossBit; bit <= OtherBit; bit++)
        {
            if ((mask & (1 << bit)) == 0)
                continue;

            ulong value = ReadValue(blob, ref offset);
            if (bit == CpuBit)
                cpuValue = value;
        }

        return cpuValue;
    }

    private static ulong ReadValue(ReadOnlySpan<byte> blob, ref int offset)
    {
        if (blob.Length - offset < sizeof(ulong))
            throw new InvalidDataException($"BinaryData 長度（{blob.Length} bytes）不足以容納遮罩所指的數值。");

        ulong value = BinaryPrimitives.ReadUInt64LittleEndian(blob[offset..]);
        offset += sizeof(ulong);
        return value;
    }
}

// SruDbIdMapTable：IdIndex → 字串（應用程式路徑、套件名稱）或二進位 SID。
internal sealed class SrumIdMap
{
    private const byte SidType = 3;

    private readonly Dictionary<int, string> _names;

    public SrumIdMap(IEnumerable<SrumIdMapEntry> entries)
    {
        _names = [];
        foreach (SrumIdMapEntry entry in entries)
        {
            if (entry.Blob is { Length: > 0 } blob)
                _names[entry.Index] = entry.Type == SidType ? FormatSid(blob) : Encoding.Unicode.GetString(blob).TrimEnd('\0');
        }
    }

    public bool TryGetName(int index, [MaybeNullWhen(false)] out string name) => _names.TryGetValue(index, out name);

    private static string FormatSid(byte[] sid)
    {
        if (sid.Length < 8 || sid.Length < 8 + sid[1] * sizeof(uint))
            return Convert.ToHexString(sid);

        ulong authority = 0;
        for (int i = 2; i < 8; i++)
            authority = (authority << 8) | sid[i];

        var text = new StringBuilder("S-", 48);
        text.Append(sid[0].ToString(CultureInfo.InvariantCulture)).Append('-');
        text.Append(authority <= uint.MaxValue ? authority.ToString(CultureInfo.InvariantCulture) : $"0x{authority:x12}");
        for (int i = 0; i < sid[1]; i++)
            text.Append('-').Append(BinaryPrimitives.ReadUInt32LittleEndian(sid.AsSpan(8 + i * sizeof(uint))).ToString(CultureInfo.InvariantCulture));

        return text.ToString();
    }
}
