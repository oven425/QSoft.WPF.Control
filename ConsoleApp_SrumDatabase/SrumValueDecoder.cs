using System.Buffers.Binary;
using System.Globalization;
using System.Text;

internal sealed record SrumRawColumnValue(
    bool IsNull,
    uint ByteLength,
    byte[]? Data,
    string? Status,
    string? PreviewHex);

internal sealed record SrumDecodedValue(
    string Name,
    uint ColumnId,
    uint EsentType,
    string TypeName,
    string Status,
    object? Value,
    uint ByteLength,
    string? PreviewHex);

internal static class SrumValueDecoder
{
    private const uint Bit = 1;
    private const uint UnsignedByte = 2;
    private const uint Short = 3;
    private const uint Long = 4;
    private const uint Currency = 5;
    private const uint IeeeSingle = 6;
    private const uint IeeeDouble = 7;
    private const uint DateTimeType = 8;
    private const uint Text = 10;
    private const uint LongText = 12;
    private const uint UnsignedLong = 14;
    private const uint LongLong = 15;
    private const uint GuidType = 16;
    private const uint UnsignedShort = 17;
    private const uint Windows1252 = 1252;
    private const uint Unicode = 1200;

    private static readonly UnicodeEncoding StrictUnicode = new(false, false, true);
    private const string Windows1252ControlRange = "\u20AC\u0081\u201A\u0192\u201E\u2026\u2020\u2021\u02C6\u2030\u0160\u2039\u0152\u008D\u017D\u008F\u0090\u2018\u2019\u201C\u201D\u2022\u2013\u2014\u02DC\u2122\u0161\u203A\u0153\u009D\u017E\u0178";

    public static bool RequiresFullValue(SrumColumnSchema column) => column.EsentType switch
    {
        Bit or UnsignedByte or Short or UnsignedShort or Long or UnsignedLong or Currency or LongLong or IeeeSingle or IeeeDouble or DateTimeType or GuidType => true,
        Text or LongText => column.CodePage is 0 or Windows1252 or Unicode,
        _ => false
    };

    public static SrumDecodedValue Decode(SrumColumnSchema column, SrumRawColumnValue raw)
    {
        if (raw.IsNull)
            return Create(column, "null", null, raw.ByteLength, null);

        if (raw.Data is null)
            return Create(column, raw.Status ?? "not-decoded", null, raw.ByteLength, raw.PreviewHex);

        ReadOnlySpan<byte> bytes = raw.Data;
        try
        {
            return column.EsentType switch
            {
                Bit when HasLength(bytes, 1) && bytes[0] <= 1 => Create(column, "decoded", bytes[0] == 1, raw.ByteLength),
                UnsignedByte when HasLength(bytes, 1) => Create(column, "decoded", bytes[0], raw.ByteLength),
                Short when HasLength(bytes, sizeof(short)) => Create(column, "decoded", BinaryPrimitives.ReadInt16LittleEndian(bytes), raw.ByteLength),
                UnsignedShort when HasLength(bytes, sizeof(ushort)) => Create(column, "decoded", BinaryPrimitives.ReadUInt16LittleEndian(bytes), raw.ByteLength),
                Long when HasLength(bytes, sizeof(int)) => Create(column, "decoded", BinaryPrimitives.ReadInt32LittleEndian(bytes), raw.ByteLength),
                UnsignedLong when HasLength(bytes, sizeof(uint)) => Create(column, "decoded", BinaryPrimitives.ReadUInt32LittleEndian(bytes), raw.ByteLength),
                Currency when HasLength(bytes, sizeof(long)) => Create(column, "decoded", BinaryPrimitives.ReadInt64LittleEndian(bytes) / 10000m, raw.ByteLength),
                LongLong when HasLength(bytes, sizeof(long)) => Create(column, "decoded", BinaryPrimitives.ReadInt64LittleEndian(bytes), raw.ByteLength),
                IeeeSingle when HasLength(bytes, sizeof(float)) => DecodeSingle(column, bytes, raw.ByteLength),
                IeeeDouble when HasLength(bytes, sizeof(double)) => DecodeDouble(column, bytes, raw.ByteLength),
                DateTimeType when HasLength(bytes, sizeof(double)) => DecodeDateTime(column, bytes, raw.ByteLength),
                GuidType when HasLength(bytes, 16) => Create(column, "decoded", new System.Guid(bytes).ToString("D"), raw.ByteLength),
                Text or LongText => DecodeText(column, bytes, raw.ByteLength),
                _ when IsSupportedScalarType(column.EsentType) => Create(column, "invalid-length-or-value", null, raw.ByteLength, CreatePreview(bytes)),
                _ => Create(column, "not-decoded-type", null, raw.ByteLength, CreatePreview(bytes))
            };
        }
        catch (DecoderFallbackException)
        {
            return Create(column, "invalid-text-encoding", null, raw.ByteLength, CreatePreview(bytes));
        }
        catch (ArgumentException)
        {
            return Create(column, "invalid-value", null, raw.ByteLength, CreatePreview(bytes));
        }
        catch (OverflowException)
        {
            return Create(column, "invalid-value", null, raw.ByteLength, CreatePreview(bytes));
        }
    }

    private static SrumDecodedValue DecodeSingle(SrumColumnSchema column, ReadOnlySpan<byte> bytes, uint length)
    {
        float value = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes));
        return float.IsFinite(value)
            ? Create(column, "decoded", value, length)
            : Create(column, "decoded-special", value.ToString("R", CultureInfo.InvariantCulture), length);
    }

    private static SrumDecodedValue DecodeDouble(SrumColumnSchema column, ReadOnlySpan<byte> bytes, uint length)
    {
        double value = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(bytes));
        return double.IsFinite(value)
            ? Create(column, "decoded", value, length)
            : Create(column, "decoded-special", value.ToString("R", CultureInfo.InvariantCulture), length);
    }

    private static SrumDecodedValue DecodeDateTime(SrumColumnSchema column, ReadOnlySpan<byte> bytes, uint length)
    {
        double value = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(bytes));
        string dateTime = DateTime.FromOADate(value).ToString("O", CultureInfo.InvariantCulture);
        return Create(column, "decoded", dateTime, length);
    }

    private static SrumDecodedValue DecodeText(SrumColumnSchema column, ReadOnlySpan<byte> bytes, uint length)
    {
        string value = column.CodePage switch
        {
            0 or Windows1252 => DecodeWindows1252(bytes),
            Unicode when bytes.Length % sizeof(char) == 0 => StrictUnicode.GetString(bytes),
            Unicode => throw new ArgumentException("UTF-16 text has an odd byte length."),
            _ => throw new DecoderFallbackException("Unsupported ESENT text code page.")
        };

        return Create(column, "decoded", value, length);
    }

    private static string DecodeWindows1252(ReadOnlySpan<byte> bytes)
    {
        var characters = new char[bytes.Length];
        for (int i = 0; i < bytes.Length; i++)
        {
            byte value = bytes[i];
            characters[i] = value is >= 0x80 and <= 0x9F
                ? Windows1252ControlRange[value - 0x80]
                : (char)value;
        }

        return new string(characters);
    }

    private static bool HasLength(ReadOnlySpan<byte> bytes, int expectedLength) => bytes.Length == expectedLength;

    private static bool IsSupportedScalarType(uint type) => type is
        Bit or UnsignedByte or Short or UnsignedShort or Long or UnsignedLong or Currency or LongLong or IeeeSingle or IeeeDouble or DateTimeType or GuidType or Text or LongText;

    private static string? CreatePreview(ReadOnlySpan<byte> bytes) =>
        bytes.Length == 0 ? null : Convert.ToHexString(bytes[..Math.Min(bytes.Length, 16)]);

    private static SrumDecodedValue Create(SrumColumnSchema column, string status, object? value, uint byteLength, string? previewHex = null) =>
        new(column.Name, column.ColumnId, column.EsentType, column.TypeName, status, value, byteLength, previewHex);
}
