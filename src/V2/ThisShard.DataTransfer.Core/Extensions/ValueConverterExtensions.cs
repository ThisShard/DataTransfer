using ThisShard.Database.Mappers;
using ThisShard.Database.Mappers.Builders;
using ThisShard.Database.Models.Tables;
using ThisShard.Foundation.Converters;

namespace ThisShard.Database.Extensions;

/// <summary>
/// Расширения для IValueConverter
/// </summary>
public static class ValueConverterExtensions
{
    /// <summary>
    /// Создает маппер строк из конвертера
    /// </summary>
    public static IRowMapper CreateRowMapper(this IValueConverter converter, ITable table)
    {
        return RowMapperBuilder.Create().AddValueConverter(converter, table).Build();
    }
}