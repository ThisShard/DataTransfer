using ThisShard.Database.Core.Converters;
using ThisShard.Database.Core.Mappers.Builders;
using ThisShard.Database.Core.Mappers.Middlewares;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Tables;

namespace ThisShard.Database.Core.Extensions;

/// <summary>
/// Расширения для билдера строк
/// </summary>
public static class RowMapperBuilderExtensions
{
    /// <summary>
    /// Добавить конвертер значений
    /// </summary>
    public static RowMapperBuilder AddValueConverter(this RowMapperBuilder builder, IValueConverter converter, ITable table) =>
        builder.AddMiddleware(new ValueConverterMiddleware(converter, table));
}