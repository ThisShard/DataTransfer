using ThisShard.Database.Mappers.Builders;
using ThisShard.Database.Mappers.Middlewares;
using ThisShard.Database.Models.Tables;
using ThisShard.Foundation.Converters;

namespace ThisShard.Database.Extensions;

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