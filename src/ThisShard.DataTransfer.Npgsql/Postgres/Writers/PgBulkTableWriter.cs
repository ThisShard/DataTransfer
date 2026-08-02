using Npgsql;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Columns;
using ThisShard.Database.Core.Models.Filters;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Schemas;
using ThisShard.Database.Core.Writers;
using ThisShard.Database.Infrastructure.Postgres.Models;
using ThisShard.Database.Infrastructure.Postgres.Tables;

namespace ThisShard.Database.Infrastructure.Postgres.Writers;

public class PgBulkTableWriter : RelationalBulkTableWriter
{
    private static readonly TableSchemaProvider SchemaProvider = new((_, i) => $"@col_{i}");
    private static readonly StagingTableSchemaProvider StagingSchemaProvider = new();
    
    private readonly NpgsqlConnection _connection;
    private readonly IPgStagingTableManager _pgStagingTableManager;
    private readonly Func<CommandFilterFactoryContext, CommandFilter<NpgsqlParameter>?>? _filterFactory;

    private CommandFilter<NpgsqlParameter>? _filter;
    private CommandFilter<NpgsqlParameter>? _insertOrUpdateFilter;

    private bool _ownsStagingTable;
    
    // ReSharper disable once ConvertToPrimaryConstructor
    public PgBulkTableWriter(
        NpgsqlConnection connection, 
        IPgStagingTableManager stagingTableManager, 
        int bufferSize, 
        Func<CommandFilterFactoryContext, CommandFilter<NpgsqlParameter>?>? filterFactory = null
        ) : base(bufferSize)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _pgStagingTableManager = stagingTableManager ?? throw new ArgumentNullException(nameof(stagingTableManager));
        _filterFactory = filterFactory;
    }

    /// <summary>
    /// Действие при инициализации таблицей
    /// </summary>
    protected override async ValueTask OnInit(ITable table)
    {
        var pgTable = table as PgTable;
        if (pgTable == null)
            throw new ArgumentOutOfRangeException(nameof(table));
        
        var stagingTable = await _pgStagingTableManager.CreateStagingTable(_connection, pgTable);
        
        StagingTableSchema = StagingSchemaProvider.GetSchema(stagingTable);
        TableSchema = SchemaProvider.GetSchema(table);
        
        _ownsStagingTable = true;
        
        InitFilter(StagingTableSchema);
    }

    /// <summary>
    /// Действие при инициализации временной таблицей
    /// </summary>
    protected override ValueTask OnInit(IStagingTable stagingTable)
    {
        if (!(stagingTable is PgStagingTable))
            throw new ArgumentOutOfRangeException(nameof(stagingTable));

        StagingTableSchema = StagingSchemaProvider.GetSchema(stagingTable);
        TableSchema = SchemaProvider.GetSchema(stagingTable.DestinationTable);

        _ownsStagingTable = false;
        
        InitFilter(StagingTableSchema);
        
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Инициализация фильтра
    /// </summary>
    private void InitFilter(IStagingTableSchema schema)
    {
        if (_filterFactory == null)
            return;

        var destColumnsDict = schema.Table.DestinationTable.Columns.ToDictionary(x => x.Key);
        var srcColumnsDict = schema.Columns
            .ToDictionary(x => x.DestinationColumn.Key);

        _filter = _filterFactory?.Invoke(new CommandFilterFactoryContext()
        {
            GetSourceColumnPath = key => $"src.{srcColumnsDict[key].ValueColumn.Path}",
            GetTargetColumnPath = key => $"dest.{destColumnsDict[key].Path}",
        });
        
        _insertOrUpdateFilter = _filterFactory?.Invoke(new CommandFilterFactoryContext()
        {
            GetSourceColumnPath = key => $"EXCLUDED.{destColumnsDict[key].Path}",
            GetTargetColumnPath = key => $"dest.{destColumnsDict[key].Path}",
        });
    }

    /// <summary>
    /// Действие при принудительной записи
    /// </summary>
    protected override async ValueTask OnFlush()
    {
        if (Buffer.Count == 0)
            return;

        var batchId = Guid.NewGuid();
        
        await WriteDataToStagingTable(batchId);
        var count = await ProcessStagingTable(batchId);
        await CleanupStagingTable(batchId);
        
        ValidateCount(count);

        ClearBuffer();
    }

    /// <summary>
    /// Действие при завершении записи
    /// </summary>
    protected override async ValueTask OnComplete()
    {
        await Flush();
        await DeleteStagingTable();
    }

    /// <summary>
    /// Действие при очистке
    /// </summary>
    protected override async ValueTask OnDispose()
    {
        await DeleteStagingTable();
    }

    #region Запись данных во временную таблицу

    /// <summary>
    /// Записывает данные во временную таблицу
    /// </summary>
    private async Task WriteDataToStagingTable(Guid batchId)
    {
        await using var writer = await _connection.BeginBinaryImportAsync(GetCopyCommandText());

        foreach (var row in Buffer)
        {
            await writer.StartRowAsync();
            
            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            foreach (PgStagingColumn column in StagingTableSchema.StagingColumns)
            {
                await WriteCell(writer, row, column, batchId);
            }
        }
        
        await writer.CompleteAsync();
    }

    /// <summary>
    /// Записывает ячейку
    /// </summary>
    private async Task WriteCell(NpgsqlBinaryImporter writer, IRow row, PgStagingColumn column, Guid batchId)
    {
        switch (column.StagingColumnType)
        {
            case StagingColumnType.BatchId:
                await writer.WriteAsync(batchId);
                break;
            case StagingColumnType.RowState:
                await writer.WriteAsync(GetDbRowState(row.State));
                break;
            case StagingColumnType.DataModificationFlag:
                await writer.WriteAsync(row.TryGetValue(column.Key, out _));
                break;
            case StagingColumnType.Data:
                row.TryGetValue(column.Key, out var value);
                await writer.WriteAsync(value, column.DataTypeName);
                break;
            default:
                await writer.WriteNullAsync();
                break;
        }
    }

    #endregion
    
    #region Обработка данных из временной таблицы

    /// <summary>
    /// Обрабатывает временную таблицу
    /// </summary>
    private async Task<int> ProcessStagingTable(Guid batchId)
    {
        var count = 0;
        
        if (Buffer.Any(x => x.State == RowState.Added))
            count += await PerformInsert(batchId);
        
        if (Buffer.Any(x => x.State == RowState.Modified))
            count += await PerformUpdate(batchId);
        
        if (Buffer.Any(x => x.State == RowState.AddedOrModified))
            count += await PerformInsertOrUpdate(batchId);
        
        if (Buffer.Any(x => x.State == RowState.Deleted || x.State == RowState.SafeDeleted))
            count += await PerformDelete(batchId);
        
        return count;
    }

    /// <summary>
    /// Выполняет команду вставки
    /// </summary>
    private async Task<int> PerformInsert(Guid batchId)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = GetInsertCommandText();
        command.Parameters.Add(new NpgsqlParameter("@batchId", batchId));
        command.Parameters.Add(new NpgsqlParameter("@state", GetDbRowState(RowState.Added)));
        return await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Выполняет команду обновления
    /// </summary>
    private async Task<int> PerformUpdate(Guid batchId)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = GetUpdateCommandText();
        command.Parameters.Add(new NpgsqlParameter("@batchId", batchId));
        command.Parameters.Add(new NpgsqlParameter("@state", GetDbRowState(RowState.Modified)));
        AppendFilterParameters(command, _filter);
        return await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Выполняет команду вставки или обновления
    /// </summary>
    private async Task<int> PerformInsertOrUpdate(Guid batchId)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = GetInsertOrUpdateCommandText();
        command.Parameters.Add(new NpgsqlParameter("@batchId", batchId));
        command.Parameters.Add(new NpgsqlParameter("@state", GetDbRowState(RowState.AddedOrModified)));
        AppendFilterParameters(command, _insertOrUpdateFilter);
        return await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Выполняет команду удаления
    /// </summary>
    private async Task<int> PerformDelete(Guid batchId)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = GetDeleteCommandText();
        command.Parameters.Add(new NpgsqlParameter("@batchId", batchId));
        command.Parameters.Add(new NpgsqlParameter("@state", GetDbRowState(RowState.Deleted)));
        AppendFilterParameters(command, _filter);
        return await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Добавляет параметры из фильтра
    /// </summary>
    private void AppendFilterParameters(NpgsqlCommand command, CommandFilter<NpgsqlParameter>? filter)
    {
        if (filter == null)
            return;

        foreach (var parameter in filter.Parameters)
        {
            command.Parameters.Add(parameter());
        }
    }

    #endregion

    #region Очистка данных временной таблицы

    /// <summary>
    /// Очищает временную таблицу по Id батча
    /// </summary>
    private async Task CleanupStagingTable(Guid batchId)
    {
        await using var command = _connection.CreateCommand();

        if (_ownsStagingTable)
        {
            command.CommandText = GetTruncateCommandText();
        }
        else
        {
            command.CommandText = GetCleanupCommandText();
        
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@batchId";
            parameter.Value = batchId;
            command.Parameters.Add(parameter);
        }
        
        await command.ExecuteNonQueryAsync();
    }

    #endregion

    #region Тексты команд

    private string? _copyCommandText;
    private string? _cleanupCommandText;
    private string? _truncateCommandText;
    
    private string? _insertCommandText;
    private string? _updateCommandText;
    private string? _insertOrUpdateCommandText;
    private string? _deleteCommandText;

    /// <summary>
    /// Возвращает текст команды копирования
    /// </summary>
    private string GetCopyCommandText() => LazyInitializer.EnsureInitialized(ref _copyCommandText, () =>
    {
        var names = string.Join(", ", StagingTableSchema.StagingColumns.Select(x => x.Path));
        return $"COPY {StagingTableSchema.Table.Path} ({names}) FROM STDIN (FORMAT BINARY)";
    });
    
    /// <summary>
    /// Возвращает текст команды удаления
    /// </summary>
    private string GetCleanupCommandText() => LazyInitializer.EnsureInitialized(ref _cleanupCommandText,
        () => $"DELETE FROM {StagingTableSchema.Table.Path} WHERE {StagingTableSchema.BatchIdColumn.Path} = @batchId");
    
    /// <summary>
    /// Возвращает текст команды очистки
    /// </summary>
    private string GetTruncateCommandText() => LazyInitializer.EnsureInitialized(ref _truncateCommandText,
        () => $"TRUNCATE TABLE {StagingTableSchema.Table.Path}");

    /// <summary>
    /// Возвращает текст команды вставки
    /// </summary>
    private string GetInsertCommandText() => LazyInitializer.EnsureInitialized(ref _insertCommandText, () =>
    {
        var destinationNames = string.Join(", ", StagingTableSchema.MutableColumns.Select(x => x.DestinationColumn.Path));
        var sourceNames = string.Join(", ", StagingTableSchema.MutableColumns.Select(x => x.ValueColumn.Path));
        return $"""
                INSERT INTO {TableSchema.Table.Path} AS dest ({destinationNames}) 
                SELECT {sourceNames} 
                FROM {StagingTableSchema.Table.Path} src
                WHERE src.{StagingTableSchema.RowStateColumn.Path} = @state AND src.{StagingTableSchema.BatchIdColumn.Path} = @batchId
                """;
    });

    /// <summary>
    /// Возвращает текст команды обновления
    /// </summary>
    private string GetUpdateCommandText() => LazyInitializer.EnsureInitialized(ref _updateCommandText, () =>
    {
        var set = string.Join(", ",
            StagingTableSchema.NonPrimaryKeyColumns.Select(x =>
                $"{x.DestinationColumn.Path} = CASE WHEN src.{x.FlagColumn.Path} THEN src.{x.ValueColumn.Path} ELSE dest.{x.DestinationColumn.Path} END"));

        var conditions =
            StagingTableSchema.PrimaryKeyColumns.Select(x =>
                $"dest.{x.DestinationColumn.Path} = src.{x.ValueColumn.Path}");

        if (_filter != null)
            conditions = conditions.Concat(Enumerable.Repeat($"({_filter.CommandFilterText})", 1));
        
        var where = string.Join(" AND ", conditions);
        
        return $"""
                UPDATE {TableSchema.Table.Path} as dest 
                SET {set}
                FROM {StagingTableSchema.Table.Path} as src
                WHERE {where} AND src.{StagingTableSchema.RowStateColumn.Path} = @state AND src.{StagingTableSchema.BatchIdColumn.Path} = @batchId
                """;
    });

    /// <summary>
    /// Возвращает текст команды добавления или обновления
    /// </summary>
    private string GetInsertOrUpdateCommandText() => LazyInitializer.EnsureInitialized(ref _insertOrUpdateCommandText, () =>
    {
        var primaryKey = string.Join(", ", StagingTableSchema.PrimaryKeyColumns.Select(x=>x.DestinationColumn.Path));
        if (!TableSchema.CanUpdate)
            return $"""
                    {GetInsertCommandText()}
                    ON CONFLICT ({primaryKey})
                    DO NOTHING
                    """;
        
        var set = string.Join(", ",
            StagingTableSchema.NonPrimaryKeyColumns.Select(x =>
                $"{x.DestinationColumn.Path} = EXCLUDED.{x.DestinationColumn.Path}"));
        
        var query = $"""
                {GetInsertCommandText()}
                ON CONFLICT ({primaryKey})
                DO UPDATE SET {set}
                """;
        
        if (_insertOrUpdateFilter != null)
            query += $" WHERE {_insertOrUpdateFilter.CommandFilterText}";
        
        return query;
    });

    /// <summary>
    /// Возвращает текст команды удаления
    /// </summary>
    private string GetDeleteCommandText() => LazyInitializer.EnsureInitialized(ref _deleteCommandText, () =>
    {
        var conditions =
            StagingTableSchema.PrimaryKeyColumns.Select(x =>
                $"dest.{x.DestinationColumn.Path} = src.{x.ValueColumn.Path}");

        if (_filter != null)
            conditions = conditions.Concat(Enumerable.Repeat($"({_filter.CommandFilterText})", 1));
        
        var where = string.Join(" AND ", conditions);
        
        return $"""
                DELETE FROM {TableSchema.Table.Path} as dest 
                USING {StagingTableSchema.Table.Path} as src
                WHERE {where} AND src.{StagingTableSchema.RowStateColumn.Path} = @state AND src.{StagingTableSchema.BatchIdColumn.Path} = @batchId
                """;
    });

    #endregion

    #region Мэппинги

    /// <summary>
    /// Возвращает стейт для записи в БД
    /// </summary>
    private int GetDbRowState(RowState rowState)
    {
        return rowState switch
        {
            RowState.SafeDeleted => (int)RowState.Deleted,
            _ => (int)rowState
        };
    }

    #endregion
    
    #region Валидация

    /// <summary>
    /// Валидация после исполнения
    /// </summary>
    protected override void ValidateCount(int count)
    {
        if (_filter != null || _insertOrUpdateFilter != null)
            return;
        
        base.ValidateCount(count);
    }

    #endregion
    
    /// <summary>
    /// Удаление временной таблицы
    /// </summary>
    private async Task DeleteStagingTable()
    {
        if (StagingTableSchema != null! && _ownsStagingTable && _connection.IsOpen())
        {
            //Пытаемся удалить временную таблицу
            try
            {
                await _pgStagingTableManager.DeleteStagingTable(_connection, (PgStagingTable)StagingTableSchema.Table);
            }
            catch (PostgresException)
            {
                //Если не получается, то игнорируем ошибку
            }
        }
        
        StagingTableSchema = null!;
        TableSchema = null!;
    }
}