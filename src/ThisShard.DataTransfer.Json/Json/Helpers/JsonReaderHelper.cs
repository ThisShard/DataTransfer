using System.Text.Json;

namespace ThisShard.Database.Infrastructure.Json.Helpers;

internal static class JsonReaderHelper
{
    public static object? GetValue(object? obj)
    {
        if (obj is JsonElement jsonObject)
            return GetValue(jsonObject);
        
        return obj;
    }

    public static object? GetValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.False:
            case JsonValueKind.True:
                return element.GetBoolean();
            
            case JsonValueKind.Null:
                return null;
            
            case JsonValueKind.Number:
                var stringValue = element.GetRawText();
                if (stringValue.Contains('e') || stringValue.Contains('E'))
                    return element.GetDouble();

                if (stringValue.Contains('.'))
                    return element.GetDecimal();
                
                if (element.TryGetInt64(out var longValue))
                    return longValue;

                if (!stringValue.Contains('-') && element.TryGetUInt64(out var ulongValue))
                    return ulongValue;
                
                return element.GetDecimal();
            
            case JsonValueKind.String:
                return element.GetString();
        }
        
        throw new ArgumentException($"Unexpected JsonValueKind: {element.ValueKind}", nameof(element));
    }
}