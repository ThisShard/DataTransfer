using System.Globalization;
using System.Text.Json;

namespace ThisShard.Database.Core.Converters.Handlers;

/// <summary>
/// Конвертеры по умолчанию
/// </summary>
public static class DefaultValueConverterHandlers
{
    #region Строки

    /// <summary>
    /// Хендлеры в String
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> ToStringHandlers { get; } =
    [
        ValueConverterHandler.CreateTyped<Guid, string>(val => val.ToString()),
        ValueConverterHandler.CreateTyped<DateTime, string>(val => val.ToString("O")),
        ValueConverterHandler.CreateTyped<DateTimeOffset, string>(val => val.ToString("O")),
        ValueConverterHandler.CreateTyped<TimeSpan, string>(val => val.ToString("c")),
        ValueConverterHandler.CreateTyped<DateOnly, string>(val => val.ToString("O")),
        ValueConverterHandler.CreateTyped<TimeOnly, string>(val => val.ToString("O")),
        ValueConverterHandler.CreateTyped<decimal, string>(val => val.ToString(CultureInfo.InvariantCulture)),
        ValueConverterHandler.CreateTyped<double, string>(val => val.ToString(CultureInfo.InvariantCulture)),
        ValueConverterHandler.CreateTyped<float, string>(val => val.ToString(CultureInfo.InvariantCulture)),
        ValueConverterHandler.CreateTyped<long, string>(val => val.ToString(CultureInfo.InvariantCulture)),
        ValueConverterHandler.CreateTyped<int, string>(val => val.ToString(CultureInfo.InvariantCulture)),
        ValueConverterHandler.CreateTyped<short, string>(val => val.ToString(CultureInfo.InvariantCulture)),
        ValueConverterHandler.CreateTyped<byte, string>(val => val.ToString(CultureInfo.InvariantCulture)),
        ValueConverterHandler.CreateTyped<ulong, string>(val => val.ToString(CultureInfo.InvariantCulture)),
        ValueConverterHandler.CreateTyped<uint, string>(val => val.ToString(CultureInfo.InvariantCulture)),
        ValueConverterHandler.CreateTyped<ushort, string>(val => val.ToString(CultureInfo.InvariantCulture)),
        ValueConverterHandler.CreateTyped<sbyte, string>(val => val.ToString(CultureInfo.InvariantCulture)),
        ValueConverterHandler.CreateTyped<char, string>(val => val.ToString()),
        ValueConverterHandler.CreateTyped<byte[], string>(Convert.ToBase64String),
        ValueConverterHandler.CreateTo<string>((val, _) => JsonSerializer.Serialize(val))
    ];

    /// <summary>
    /// Хендлеры из String
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> FromStringHandlers { get; } =
    [
        ValueConverterHandler.CreateFrom<string>((val, type) => JsonSerializer.Deserialize(val, type))
    ];
    
    #endregion

    #region Guid

    /// <summary>
    /// Хендлеры в Guid
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> ToGuidHandlers { get; } =
    [
        ValueConverterHandler.CreateTypedNullable<string, Guid>(val => Guid.TryParse(val, out var dt) ? dt : null),
    ];

    #endregion

    #region Дата/Время

    /// <summary>
    /// Хендлеры в DateTime
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> ToDateTimeHandlers { get; } =
    [
        ValueConverterHandler.CreateTypedNullable<string, DateTime>(val => DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt) ? dt : null),
        ValueConverterHandler.CreateTyped<DateTimeOffset, DateTime>(val => val.DateTime),
    ];
    
    /// <summary>
    /// Хендлеры в DateTimeOffset
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> ToDateTimeOffsetHandlers { get; } =
    [
        ValueConverterHandler.CreateTypedNullable<string, DateTimeOffset>(val => DateTimeOffset.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt) ? dt : null),
        ValueConverterHandler.CreateTyped<DateTime, DateTimeOffset>(val => new DateTimeOffset(val)),
    ];

    /// <summary>
    /// Хендлеры в TimeSpan
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> ToTimeSpanHandlers { get; } =
    [
        ValueConverterHandler.CreateTypedNullable<string, TimeSpan>(val => TimeSpan.TryParse(val, CultureInfo.InvariantCulture, out var dt) ? dt : null),
    ];
    
    /// <summary>
    /// Хендлеры в DateOnly
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> ToDateOnlyHandlers { get; } =
    [
        ValueConverterHandler.CreateTypedNullable<string, DateOnly>(val => DateOnly.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt) ? dt : null),
    ];
    
    /// <summary>
    /// Хендлеры в TimeOnly
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> ToTimeOnlyHandlers { get; } =
    [
        ValueConverterHandler.CreateTypedNullable<string, TimeOnly>(val => TimeOnly.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt) ? dt : null),
    ];
    
    #endregion

    #region Целые числа

    /// <summary>
    /// Хендлеры в Long
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> ToLongHandlers { get; } =
    [
        ValueConverterHandler.CreateTypedNullable<string, long>(val => long.TryParse(val, CultureInfo.InvariantCulture, out var dt) ? dt : null),
        //ValueConverterHandler.CreateTyped<long, long>(val => val),
        ValueConverterHandler.CreateTyped<ulong, long>(val => (long)val),
        ValueConverterHandler.CreateTyped<int, long>(val => val),
        ValueConverterHandler.CreateTyped<uint, long>(val => val),
        ValueConverterHandler.CreateTyped<short, long>(val => val),
        ValueConverterHandler.CreateTyped<ushort, long>(val => val),
        ValueConverterHandler.CreateTyped<byte, long>(val => val),
        ValueConverterHandler.CreateTyped<sbyte, long>(val => val),
        ValueConverterHandler.CreateTyped<bool, long>(val => val ? 1L : 0L),
        ValueConverterHandler.CreateTyped<decimal, long>(val => (long)val),
        ValueConverterHandler.CreateTyped<double, long>(val => (long)val),
        ValueConverterHandler.CreateTyped<float, long>(val => (long)val),
    ];
    
    /// <summary>
    /// Хендлеры в ULong
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> ToULongHandlers { get; } =
    [
        ValueConverterHandler.CreateTypedNullable<string, ulong>(val => ulong.TryParse(val, CultureInfo.InvariantCulture, out var dt) ? dt : null),
        ValueConverterHandler.CreateTyped<long, ulong>(val => (ulong)val),
        //ValueConverterHandler.CreateTyped<ulong, ulong>(val => val),
        ValueConverterHandler.CreateTyped<int, ulong>(val => (ulong)val),
        ValueConverterHandler.CreateTyped<uint, ulong>(val => val),
        ValueConverterHandler.CreateTyped<short, ulong>(val => (ulong)val),
        ValueConverterHandler.CreateTyped<ushort, ulong>(val => val),
        ValueConverterHandler.CreateTyped<byte, ulong>(val => val),
        ValueConverterHandler.CreateTyped<sbyte, ulong>(val => (ulong)val),
        ValueConverterHandler.CreateTyped<bool, ulong>(val => val ? 1UL : 0UL),
        ValueConverterHandler.CreateTyped<decimal, ulong>(val => (ulong)val),
        ValueConverterHandler.CreateTyped<double, ulong>(val => (ulong)val),
        ValueConverterHandler.CreateTyped<float, ulong>(val => (ulong)val),
    ];
    
    /// <summary>
    /// Хендлеры в Int
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> ToIntHandlers { get; } =
    [
        ValueConverterHandler.CreateTypedNullable<string, int>(val => int.TryParse(val, CultureInfo.InvariantCulture, out var dt) ? dt : null),
        ValueConverterHandler.CreateTyped<long, int>(val => (int)val),
        ValueConverterHandler.CreateTyped<ulong, int>(val => (int)val),
        //ValueConverterHandler.CreateTyped<int, int>(val => val),
        ValueConverterHandler.CreateTyped<uint, int>(val => (int)val),
        ValueConverterHandler.CreateTyped<short, int>(val => val),
        ValueConverterHandler.CreateTyped<ushort, int>(val => val),
        ValueConverterHandler.CreateTyped<byte, int>(val => val),
        ValueConverterHandler.CreateTyped<sbyte, int>(val => val),
        ValueConverterHandler.CreateTyped<bool, int>(val => val ? 1 : 0),
        ValueConverterHandler.CreateTyped<decimal, int>(val => (int)val),
        ValueConverterHandler.CreateTyped<double, int>(val => (int)val),
        ValueConverterHandler.CreateTyped<float, int>(val => (int)val),
    ];
    
    /// <summary>
    /// Хендлеры в UInt
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> ToUIntHandlers { get; } =
    [
        ValueConverterHandler.CreateTypedNullable<string, uint>(val => uint.TryParse(val, CultureInfo.InvariantCulture, out var dt) ? dt : null),
        ValueConverterHandler.CreateTyped<long, uint>(val => (uint)val),
        ValueConverterHandler.CreateTyped<ulong, uint>(val => (uint)val),
        ValueConverterHandler.CreateTyped<int, uint>(val => (uint)val),
        //ValueConverterHandler.CreateTyped<uint, uint>(val => val),
        ValueConverterHandler.CreateTyped<short, uint>(val => (uint)val),
        ValueConverterHandler.CreateTyped<ushort, uint>(val => val),
        ValueConverterHandler.CreateTyped<byte, uint>(val => val),
        ValueConverterHandler.CreateTyped<sbyte, uint>(val => (uint)val),
        ValueConverterHandler.CreateTyped<bool, uint>(val => val ? 1U : 0U),
        ValueConverterHandler.CreateTyped<decimal, uint>(val => (uint)val),
        ValueConverterHandler.CreateTyped<double, uint>(val => (uint)val),
        ValueConverterHandler.CreateTyped<float, uint>(val => (uint)val),
    ];
    
    /// <summary>
    /// Хендлеры в Short
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> ToShortHandlers { get; } =
    [
        ValueConverterHandler.CreateTypedNullable<string, short>(val => short.TryParse(val, CultureInfo.InvariantCulture, out var dt) ? dt : null),
        ValueConverterHandler.CreateTyped<long, short>(val => (short)val),
        ValueConverterHandler.CreateTyped<ulong, short>(val => (short)val),
        ValueConverterHandler.CreateTyped<int, short>(val => (short)val),
        ValueConverterHandler.CreateTyped<uint, short>(val => (short)val),
        //ValueConverterHandler.CreateTyped<short, short>(val => val),
        ValueConverterHandler.CreateTyped<ushort, short>(val => (short)val),
        ValueConverterHandler.CreateTyped<byte, short>(val => val),
        ValueConverterHandler.CreateTyped<sbyte, short>(val => val),
        ValueConverterHandler.CreateTyped<bool, short>(val => val ? (short)1 : (short)0),
        ValueConverterHandler.CreateTyped<decimal, short>(val => (short)val),
        ValueConverterHandler.CreateTyped<double, short>(val => (short)val),
        ValueConverterHandler.CreateTyped<float, short>(val => (short)val),
    ];
    
    /// <summary>
    /// Хендлеры в UShort
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> ToUShortHandlers { get; } =
    [
        ValueConverterHandler.CreateTypedNullable<string, ushort>(val => ushort.TryParse(val, CultureInfo.InvariantCulture, out var dt) ? dt : null),
        ValueConverterHandler.CreateTyped<long, ushort>(val => (ushort)val),
        ValueConverterHandler.CreateTyped<ulong, ushort>(val => (ushort)val),
        ValueConverterHandler.CreateTyped<int, ushort>(val => (ushort)val),
        ValueConverterHandler.CreateTyped<uint, ushort>(val => (ushort)val),
        ValueConverterHandler.CreateTyped<short, ushort>(val => (ushort)val),
        //ValueConverterHandler.CreateTyped<ushort, ushort>(val => val),
        ValueConverterHandler.CreateTyped<byte, ushort>(val => val),
        ValueConverterHandler.CreateTyped<sbyte, ushort>(val => (ushort)val),
        ValueConverterHandler.CreateTyped<bool, ushort>(val => val ? (ushort)1 : (ushort)0),
        ValueConverterHandler.CreateTyped<decimal, ushort>(val => (ushort)val),
        ValueConverterHandler.CreateTyped<double, ushort>(val => (ushort)val),
        ValueConverterHandler.CreateTyped<float, ushort>(val => (ushort)val),
    ];
    
    /// <summary>
    /// Хендлеры в Byte
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> ToByteHandlers { get; } =
    [
        ValueConverterHandler.CreateTypedNullable<string, byte>(val => byte.TryParse(val, CultureInfo.InvariantCulture, out var dt) ? dt : null),
        ValueConverterHandler.CreateTyped<long, byte>(val => (byte)val),
        ValueConverterHandler.CreateTyped<ulong, byte>(val => (byte)val),
        ValueConverterHandler.CreateTyped<int, byte>(val => (byte)val),
        ValueConverterHandler.CreateTyped<uint, byte>(val => (byte)val),
        ValueConverterHandler.CreateTyped<short, byte>(val => (byte)val),
        ValueConverterHandler.CreateTyped<ushort, byte>(val => (byte)val),
        //ValueConverterHandler.CreateTyped<byte, byte>(val => val),
        ValueConverterHandler.CreateTyped<sbyte, byte>(val => (byte)val),
        ValueConverterHandler.CreateTyped<bool, byte>(val => val ? (byte)1 : (byte)0),
        ValueConverterHandler.CreateTyped<decimal, byte>(val => (byte)val),
        ValueConverterHandler.CreateTyped<double, byte>(val => (byte)val),
        ValueConverterHandler.CreateTyped<float, byte>(val => (byte)val),
    ];
    
    /// <summary>
    /// Хендлеры в SByte
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> ToSByteHandlers { get; } =
    [
        ValueConverterHandler.CreateTypedNullable<string, sbyte>(val => sbyte.TryParse(val, CultureInfo.InvariantCulture, out var dt) ? dt : null),
        ValueConverterHandler.CreateTyped<long, sbyte>(val => (sbyte)val),
        ValueConverterHandler.CreateTyped<ulong, sbyte>(val => (sbyte)val),
        ValueConverterHandler.CreateTyped<int, sbyte>(val => (sbyte)val),
        ValueConverterHandler.CreateTyped<uint, sbyte>(val => (sbyte)val),
        ValueConverterHandler.CreateTyped<short, sbyte>(val => (sbyte)val),
        ValueConverterHandler.CreateTyped<ushort, sbyte>(val => (sbyte)val),
        ValueConverterHandler.CreateTyped<byte, sbyte>(val => (sbyte)val),
        //ValueConverterHandler.CreateTyped<sbyte, sbyte>(val => val),
        ValueConverterHandler.CreateTyped<bool, sbyte>(val => val ? (sbyte)1 : (sbyte)0),
        ValueConverterHandler.CreateTyped<decimal, sbyte>(val => (sbyte)val),
        ValueConverterHandler.CreateTyped<double, sbyte>(val => (sbyte)val),
        ValueConverterHandler.CreateTyped<float, sbyte>(val => (sbyte)val),
    ];
    
    /// <summary>
    /// Хендлеры в Bool
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> ToBoolHandlers { get; } =
    [
        ValueConverterHandler.CreateTypedNullable<string, bool>(val => bool.TryParse(val, out var dt) ? dt : null),
        ValueConverterHandler.CreateTyped<long, bool>(val => val > 0),
        ValueConverterHandler.CreateTyped<ulong, bool>(val => val > 0),
        ValueConverterHandler.CreateTyped<int, bool>(val => val > 0),
        ValueConverterHandler.CreateTyped<uint, bool>(val => val > 0),
        ValueConverterHandler.CreateTyped<short, bool>(val => val > 0),
        ValueConverterHandler.CreateTyped<ushort, bool>(val => val > 0),
        ValueConverterHandler.CreateTyped<byte, bool>(val => val > 0),
        ValueConverterHandler.CreateTyped<sbyte, bool>(val => val > 0),
        ValueConverterHandler.CreateTyped<decimal, bool>(val => val > 0),
        ValueConverterHandler.CreateTyped<double, bool>(val => val > 0),
        ValueConverterHandler.CreateTyped<float, bool>(val => val > 0),
    ];
    
    #endregion

    #region Дробные числа

    /// <summary>
    /// Хендлеры в Double
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> ToDoubleHandlers { get; } =
    [
        ValueConverterHandler.CreateTypedNullable<string, double>(val => double.TryParse(val, CultureInfo.InvariantCulture, out var dt) ? dt : null),
        //ValueConverterHandler.CreateTyped<double, double>(val => val),
        ValueConverterHandler.CreateTyped<decimal, double>(val => (double)val),
        ValueConverterHandler.CreateTyped<float, double>(val => val),
        ValueConverterHandler.CreateTyped<long, double>(val => val),
        ValueConverterHandler.CreateTyped<ulong, double>(val => val),
        ValueConverterHandler.CreateTyped<int, double>(val => val),
        ValueConverterHandler.CreateTyped<uint, double>(val => val),
        ValueConverterHandler.CreateTyped<short, double>(val => val),
        ValueConverterHandler.CreateTyped<ushort, double>(val => val),
        ValueConverterHandler.CreateTyped<byte, double>(val => val),
        ValueConverterHandler.CreateTyped<sbyte, double>(val => val),
        ValueConverterHandler.CreateTyped<bool, double>(val => val ? 1.0 : 0.0),
    ];

    /// <summary>
    /// Хендлеры в Float
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> ToFloatHandlers { get; } =
    [
        ValueConverterHandler.CreateTypedNullable<string, float>(val => float.TryParse(val, CultureInfo.InvariantCulture, out var dt) ? dt : null),
        ValueConverterHandler.CreateTyped<double, float>(val => (float)val),
        ValueConverterHandler.CreateTyped<decimal, float>(val => (float)val),
        //ValueConverterHandler.CreateTyped<float, float>(val => val),
        ValueConverterHandler.CreateTyped<long, float>(val => val),
        ValueConverterHandler.CreateTyped<ulong, float>(val => val),
        ValueConverterHandler.CreateTyped<int, float>(val => val),
        ValueConverterHandler.CreateTyped<uint, float>(val => val),
        ValueConverterHandler.CreateTyped<short, float>(val => val),
        ValueConverterHandler.CreateTyped<ushort, float>(val => val),
        ValueConverterHandler.CreateTyped<byte, float>(val => val),
        ValueConverterHandler.CreateTyped<sbyte, float>(val => val),
        ValueConverterHandler.CreateTyped<bool, float>(val => val ? 1f: 0f),
    ];
    
    /// <summary>
    /// Хендлеры в Decimal
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> ToDecimalHandlers { get; } =
    [
        ValueConverterHandler.CreateTypedNullable<string, decimal>(val => decimal.TryParse(val, CultureInfo.InvariantCulture, out var dt) ? dt : null),
        ValueConverterHandler.CreateTyped<double, decimal>(val => (decimal)val),
        //ValueConverterHandler.CreateTyped<decimal, decimal>(val => val),
        ValueConverterHandler.CreateTyped<float, decimal>(val => (decimal)val),
        ValueConverterHandler.CreateTyped<long, decimal>(val => val),
        ValueConverterHandler.CreateTyped<ulong, decimal>(val => val),
        ValueConverterHandler.CreateTyped<int, decimal>(val => val),
        ValueConverterHandler.CreateTyped<uint, decimal>(val => val),
        ValueConverterHandler.CreateTyped<short, decimal>(val => val),
        ValueConverterHandler.CreateTyped<ushort, decimal>(val => val),
        ValueConverterHandler.CreateTyped<byte, decimal>(val => val),
        ValueConverterHandler.CreateTyped<sbyte, decimal>(val => val),
        ValueConverterHandler.CreateTyped<bool, decimal>(val => val ? 1m: 0m),
    ];

    #endregion

    #region Массивы байт

    /// <summary>
    /// Хендлеры в Byte[]
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> ToByteArrayHandlers { get; } =
    [
        ValueConverterHandler.CreateTyped<string, byte[]>(Convert.FromBase64String),
    ];

    #endregion
    
    /// <summary>
    /// Все хендлеры по умолчанию
    /// </summary>
    public static IReadOnlyList<IValueConverterHandler> Default { get; } =
    [
        //Строки
        ..ToStringHandlers,
        ..FromStringHandlers,
        
        //Guid
        ..ToGuidHandlers,
        
        //Дата/Время
        ..ToDateTimeHandlers,
        ..ToDateTimeOffsetHandlers,
        ..ToTimeSpanHandlers,
        ..ToDateOnlyHandlers,
        ..ToTimeOnlyHandlers,
        
        //Целые числа
        ..ToLongHandlers,
        ..ToULongHandlers,
        ..ToIntHandlers,
        ..ToUIntHandlers,
        ..ToShortHandlers,
        ..ToUShortHandlers,
        ..ToByteHandlers,
        ..ToSByteHandlers,
        ..ToBoolHandlers,
        
        //Дробные числа
        ..ToDoubleHandlers,
        ..ToDecimalHandlers,
        ..ToFloatHandlers,
        
        //Массивы байт
        ..ToByteArrayHandlers
    ];
}