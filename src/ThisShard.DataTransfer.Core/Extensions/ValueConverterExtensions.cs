using ThisShard.Database.Core.Converters;
using ThisShard.Database.Core.Mappers;
using ThisShard.Database.Core.Mappers.Builders;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Core.Extensions;

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