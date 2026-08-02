using System.Text.Json;

namespace ThisShard.Database.Infrastructure.Json.Helpers;

/// <summary>
/// Хелпер для JsonWriter
/// </summary>
internal static class JsonWriterHelper
{
    /// <summary>
    /// Список поддерживаемых нативных типов для Json
    /// </summary>
    public static IReadOnlyCollection<Type> KnownTypes { get; } =
        new HashSet<Type>
        {
            typeof(string),
            typeof(Guid),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(long),
            typeof(ulong),
            typeof(int),
            typeof(uint),
            typeof(short),
            typeof(ushort),
            typeof(byte),
            typeof(sbyte),
            typeof(double),
            typeof(float),
            typeof(decimal),
            typeof(bool),
            typeof(byte[]),
            typeof(DBNull)
        };
    
    /// <summary>
    /// Возвращает тип колонки Json
    /// </summary>
    public static Type GetJsonColumnType(Type type) => KnownTypes.Contains(type) ? type : typeof(string);
    
    /// <summary>
    /// Пишет свойство со значением
    /// </summary>
    public static void WriteValue(this Utf8JsonWriter jsonWriter, string propertyName, object? value)
    {
        switch (value)
        {
            case string val:
                jsonWriter.WriteString(propertyName, val);
                break;
            
            case Guid val:
                jsonWriter.WriteString(propertyName, val);
                break;
            
            case DateTime val:
                jsonWriter.WriteString(propertyName, val);
                break;
            
            case DateTimeOffset val:
                jsonWriter.WriteString(propertyName, val);
                break;
            
            case TimeSpan val:
                jsonWriter.WriteString(propertyName, val.ToString("c"));
                break;
            
            case DateOnly val:
                jsonWriter.WriteString(propertyName, val.ToString("O"));
                break;
            
            case TimeOnly val:
                jsonWriter.WriteString(propertyName, val.ToString("O"));
                break;
            
            case long val:
                jsonWriter.WriteNumber(propertyName, val);
                break;
            
            case ulong val:
                jsonWriter.WriteNumber(propertyName, val);
                break;
            
            case int val:
                jsonWriter.WriteNumber(propertyName, val);
                break;
            
            case uint val:
                jsonWriter.WriteNumber(propertyName, val);
                break;
            
            case short val:
                jsonWriter.WriteNumber(propertyName, val);
                break;
            
            case ushort val:
                jsonWriter.WriteNumber(propertyName, val);
                break;
            
            case byte val:
                jsonWriter.WriteNumber(propertyName, val);
                break;
            
            case sbyte val:
                jsonWriter.WriteNumber(propertyName, val);
                break;
            
            case double val:
                jsonWriter.WriteNumber(propertyName, val);
                break;
            
            case float val:
                jsonWriter.WriteNumber(propertyName, val);
                break;
            
            case decimal val:
                jsonWriter.WriteNumber(propertyName, val);
                break;
            
            case char val:
                jsonWriter.WriteString(propertyName, val.ToString());
                break;
            
            case bool val:
                jsonWriter.WriteBoolean(propertyName, val);
                break;
            
            case byte[] val:
                jsonWriter.WriteBase64String(propertyName, val);
                break;
            
            case DBNull:
            case null:
                jsonWriter.WriteNull(propertyName);
                break;
            
            default:
                throw new NotSupportedException($"Unsupported type {value.GetType()}");
                break;
        }
    }
}