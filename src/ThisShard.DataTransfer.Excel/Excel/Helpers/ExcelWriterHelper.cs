using LargeXlsx;
using ThisShard.Database.Infrastructure.Excel.Options;

namespace ThisShard.Database.Infrastructure.Excel.Helpers;

/// <summary>
/// Хелпер для ExcelWriter
/// </summary>
internal static class ExcelWriterHelper
{
    /// <summary>
    /// Список поддерживаемых нативных типов для Excel
    /// </summary>
    public static IReadOnlyDictionary<Type, Type> TypesMap { get; } =
        new Dictionary<Type, Type>
        {
            [typeof(string)] = typeof(string),
            [typeof(Guid)] = typeof(string),
            [typeof(DateTimeOffset)] = typeof(string),
            [typeof(byte[])] = typeof(string),
            
            [typeof(DateTime)] = typeof(DateTime),
            
            [typeof(long)] = typeof(decimal),
            [typeof(ulong)] = typeof(decimal),
            [typeof(uint)] = typeof(decimal),
            [typeof(decimal)] = typeof(decimal),
            
            [typeof(double)] = typeof(double),
            [typeof(float)] = typeof(double),
            
            [typeof(int)] = typeof(int),
            [typeof(short)] = typeof(int),
            [typeof(ushort)] = typeof(int),
            [typeof(byte)] = typeof(int),
            [typeof(sbyte)] = typeof(int),
            
            [typeof(bool)] = typeof(bool),
            
            [typeof(DBNull)] = typeof(DBNull),
        };
    
    /// <summary>
    /// Возвращает тип колонки Excel
    /// </summary>
    public static Type GetExcelColumnType(Type type) => TypesMap.TryGetValue(type, out var columnType) ? columnType : typeof(string);
    
    /// <summary>
    /// Пишет свойство со значением
    /// </summary>
    public static void WriteValue(this XlsxWriter writer, object? value, ExcelStyleOptions styles)
    {
        switch (value)
        {
            case string val:
                writer.Write(val, styles.TextStyle);
                break;
            
            case int val:
                writer.Write(val, styles.IntegerStyle);
                break;
            
            case DateTime val:
                writer.Write(val, styles.DateTimeStyle);
                break;
            
            case decimal val:
                writer.Write(val, styles.DecimalStyle);
                break;
            
            case double val:
                writer.Write(val, styles.DoubleStyle);
                break;
            
            case bool val:
                writer.Write(val, styles.BooleanStyle);
                break;
            
            case DBNull:
            case null:
                writer.Write(styles.NullStyle);
                break;
            
            default:
                throw new NotSupportedException($"Unsupported type {value.GetType()}");
                break;
        }
    }
}