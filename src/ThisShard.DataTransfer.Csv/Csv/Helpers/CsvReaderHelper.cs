using System.Linq.Expressions;
using System.Reflection;
using CsvHelper;

namespace ThisShard.Database.Infrastructure.Csv.Helpers;

/// <summary>
/// Хелпер ридера csv
/// </summary>
internal static class CsvReaderHelper
{
    private static readonly Func<CsvParser, int, int> GetQuotesCountExpression;

    static CsvReaderHelper()
    {
        var fieldsInfo = typeof(CsvParser).GetField("fields", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var quotesInfo = fieldsInfo.FieldType.GetElementType()!.GetField("QuoteCount", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)!;
        
        var parserExpression = Expression.Parameter(typeof(CsvParser));
        var indexExpression = Expression.Parameter(typeof(int));
        var fieldAccessExpression = Expression.Field(parserExpression, fieldsInfo);
        var arrayAccessExpression = Expression.ArrayAccess(fieldAccessExpression, indexExpression);
        var quoteFieldAccessExpression = Expression.Field(arrayAccessExpression, quotesInfo);
        var lambdaExpression = Expression.Lambda<Func<CsvParser, int, int>>(quoteFieldAccessExpression, parserExpression, indexExpression);

        GetQuotesCountExpression = lambdaExpression.Compile();
    }
    
    /// <summary>
    /// Записано ли поле в кавычках
    /// </summary>
    public static bool IsFieldQuoted(this CsvParser parser, int index)
    {
        return GetQuotesCountExpression(parser, index) > 0;
    }
}