using ThisShard.Database.Core.Converters;
using ThisShard.Database.Core.Mappers;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Writers;

namespace ThisShard.Database.Core.Extensions;

public static class TableWriterExtensions
{
    /// <summary>
    /// Возвращает писатель таблиц использующий конвертер значений
    /// </summary>
    public static ITableWriter UsingValueConverter(this ITableWriter writer, IValueConverter converter)
    {
        var tableWriter = writer;
        if (tableWriter == null)
            throw new NotSupportedException("The writer does not support the ITableWriter.");
        
        return writer.UsingMapperFactory(converter.CreateRowMapper);
    }
    
    /// <summary>
    /// Возвращает писатель таблиц использующий маппер
    /// </summary>
    public static ITableWriter UsingMapper(this ITableWriter writer, IRowMapper mapper) =>
        writer.UsingConverter(row => new MappedRow(row, mapper));

    /// <summary>
    /// Возвращает писатель таблиц использующий маппер
    /// </summary>
    public static ITableWriter UsingMapperFactory(this ITableWriter writer, Func<ITable, IRowMapper> factory) =>
        writer.UsingConverterFactory(table =>
        {
            var mapper = factory(table);
            return row => new MappedRow(row, mapper);
        });
    
    /// <summary>
    /// Возвращает писатель таблиц использующий конвертер
    /// </summary>
    public static ITableWriter UsingConverter(this ITableWriter writer, Func<IRow, IRow?> converter) => 
        writer.UsingConverterFactory(_ => converter);
    
    /// <summary>
    /// Возвращает писатель таблиц использующий конвертер
    /// </summary>
    public static ITableWriter UsingConverterFactory(this ITableWriter writer, Func<ITable, Func<IRow, IRow?>> factory) => 
        new ConvertedTableWriter(writer, factory);
}