namespace ThisShard.Database.Infrastructure.Json.Helpers;

internal static class JsonNamingHelper
{
    /// <summary>
    /// Конвертирует в CamelCase с акромнимами
    /// </summary>
    public static string ToCamelCaseWithAcronyms(string input)
    {
        if (string.IsNullOrEmpty(input) || !char.IsUpper(input[0])) 
            return input;

        var chars = input.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (i > 0 && i + 1 < chars.Length && char.IsLower(chars[i + 1]))
                break;
            
            chars[i] = char.ToLowerInvariant(chars[i]);
        }
        return new string(chars);
    }
}