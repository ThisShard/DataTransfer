using LargeXlsx;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Pipelines.Builders;
using ThisShard.Database.Core.Pipelines.Destinations;
using ThisShard.Database.Core.Writers;
using ThisShard.Database.Infrastructure.Extensions;
using ThisShard.Database.Infrastructure.Excel.Models;
using ThisShard.Database.Infrastructure.Excel.Options;

namespace ThisShard.Database.Infrastructure.Excel.Pipelines.Builders;

public class ExcelPipelineDestinationBuilder : IPipelineDestinationBuilder
{
    private Type? _entityType;
    private ITable? _table;
    private ExcelBulkOperationsOptions? _options;
    private Func<ValueTask<XlsxWriter>>? _writerFactory;
    private bool _ownsWriter;
    
    /// <summary>
    /// Ключ
    /// </summary>
    public string Key { get; private set; } = string.Empty;
    
    /// <summary>
    /// Указать ключ для назначения
    /// </summary>
    IPipelineDestinationBuilder IPipelineDestinationBuilder.WithKey(string key) => WithKey(key);

    /// <summary>
    /// Указать ключ для назначения
    /// </summary>
    public ExcelPipelineDestinationBuilder WithKey(string key)
    {
        Key = key;
        return this;
    }

    /// <summary>
    /// Указать тип сущности
    /// </summary>
    public ExcelPipelineDestinationBuilder WithEntityType(Type entityType)
    {
        _entityType = entityType;
        return this;
    }
    
    /// <summary>
    /// Указать таблицу
    /// </summary>
    public ExcelPipelineDestinationBuilder WithTable(ITable table)
    {
        _table = table;
        return this;
    }

    /// <summary>
    /// Указать настройки
    /// </summary>
    public ExcelPipelineDestinationBuilder WithOptions(ExcelBulkOperationsOptions options)
    {
        _options = options;
        return this;
    }

    /// <summary>
    /// Указать писателя
    /// </summary>
    public ExcelPipelineDestinationBuilder WithWriter(XlsxWriter writer, bool ownsWriter = false)
    {
        _writerFactory = () => new ValueTask<XlsxWriter>(writer);
        _ownsWriter = ownsWriter;
        return this;
    }

    /// <summary>
    /// Указать фабрику писателей
    /// </summary>
    public ExcelPipelineDestinationBuilder WithWriterFactory(Func<ValueTask<XlsxWriter>> factory, bool ownsWriter = true)
    {
        _writerFactory = factory;
        _ownsWriter = ownsWriter;
        return this;
    }
    
    /// <summary>
    /// Билдит назначение
    /// </summary>
    public IPipelineDestination Build()
    {
        var connectionFactory = _writerFactory;
        if (connectionFactory == null)
            throw new InvalidOperationException("Writer factory must be set");

        var writerFactory = BuildWriterFactory();

        return new PipelineDestination<XlsxWriter>(
            Key,
            connectionFactory,
            writerFactory,
            null,
            _ownsWriter
        );
    }
    
    /// <summary>
    /// Билдит фабрику писателей
    /// </summary>
    private Func<XlsxWriter, ITable?, ValueTask<IRowWriter>> BuildWriterFactory()
    {
        var table = _table;
        var entityType = _entityType;
        var options = _options;

        if (table is ExcelTable excelTable)
            return async (cn, _) => await cn.GetTableWriter(excelTable, options);
        
        if (table != null)
            return async (cn, _) => await cn.GetTableWriter(table, options);
        
        if (entityType != null)
            return async (cn, _) => await cn.GetTableWriter(entityType, options);

        return async (cn, t) =>
        {
            if (t == null)
                throw new InvalidOperationException("Table must be provided");
            
            return await cn.GetTableWriter(t, options);
        };
    }
}