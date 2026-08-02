using Npgsql;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Columns;
using ThisShard.Database.Core.Models.Filters;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Schemas;
using ThisShard.Database.Core.Writers;
using ThisShard.Database.Infrastructure.Postgres.Models;

namespace ThisShard.Database.Infrastructure.Postgres.Writers;

/// <summary>
/// Писатель в таблицу Postgres использующий батчинг комманд
/// </summary>
public class PgBatchTableWriter : RelationalTableWriter
{
    private static readonly TableSchemaProvider SchemaProvider = new((_, i) => $"@col_{i}");
    
    private readonly NpgsqlConnection _connection;
    private readonly Func<CommandFilterFactoryContext, CommandFilter<NpgsqlParameter>?>? _filterFactory;

    private CommandFilter<NpgsqlParameter>? _filter;
    private CommandFilter<NpgsqlParameter>? _insertOrUpdateFilter;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PgBatchTableWriter(
        NpgsqlConnection connection, 
        int bufferSize, 
        Func<CommandFilterFactoryContext, CommandFilter<NpgsqlParameter>?>? filterFactory = null
        ) : base(bufferSize)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _filterFactory = filterFactory;
    }

    /// <summary>
    /// Действие при инициализации таблицей
    /// </summary>
    protected override ValueTask OnInit(ITable table)
    {
        var pgTable = table as PgTable;
        if (pgTable == null)
            throw new ArgumentOutOfRangeException(nameof(table));
        
        TableSchema = SchemaProvider.GetSchema(pgTable);

        InitFilter(table);

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Инициализация фильтра
    /// </summary>
    private void InitFilter(ITable table)
    {
        if (_filterFactory == null)
            return;
        
        var columnsDict = table.Columns.ToDictionary(x => x.Key);

        _filter = _filterFactory?.Invoke(new CommandFilterFactoryContext()
        {
            GetSourceColumnPath = key => TableSchema.ColumnParameterMappings[columnsDict[key]],
            GetTargetColumnPath = key => columnsDict[key].Path,
        });
        
        _insertOrUpdateFilter = _filterFactory?.Invoke(new CommandFilterFactoryContext()
        {
            GetSourceColumnPath = key => TableSchema.ColumnParameterMappings[columnsDict[key]],
            GetTargetColumnPath = key => $"{TableSchema.Table.Path}.{columnsDict[key].Path}",
        });
    }

    /// <summary>
    /// Действие при инициализации временной таблицей
    /// </summary>
    protected override ValueTask OnInit(IStagingTable stagingTable)
    {
        return OnInit(stagingTable.DestinationTable);
    }

    /// <summary>
    /// Действие при принудительной записи
    /// </summary>
    protected override async ValueTask OnFlush()
    {
        if (Buffer.Count == 0)
            return;

        await using var batch = _connection.CreateBatch();

        foreach (var row in Buffer)
        {
            var command = CreateCommand(batch, row);
            if (command != null)
                batch.BatchCommands.Add(command);
        }

        if (batch.BatchCommands.Count != 0)
        {
            var count = await batch.ExecuteNonQueryAsync();
            ValidateCount(count);
        }

        ClearBuffer();
    }

    /// <summary>
    /// Действие при завершении записи
    /// </summary>
    protected override ValueTask OnComplete()
    {
        return Flush();
    }

    /// <summary>
    /// Действие при очистке
    /// </summary>
    protected override ValueTask OnDispose()
    {
        return ValueTask.CompletedTask;
    }

    #region Создание команд

    /// <summary>
    /// Создает команду для батчинга
    /// </summary>
    private NpgsqlBatchCommand? CreateCommand(NpgsqlBatch batch, IRow row)
    {
        switch (row.State)
        {
            case RowState.AddedOrModified when TableSchema is { CanInsert: true, CanUpdate: true }:
                return CreateInsertOrUpdateCommand(batch, row);
            case RowState.AddedOrModified when TableSchema.CanInsert:
            case RowState.Added:
                return CreateInsertCommand(batch, row);
            case RowState.AddedOrModified when TableSchema.CanUpdate:
            case RowState.Modified:
                return CreateUpdateCommand(batch, row);
            case RowState.Deleted:
            case RowState.SafeDeleted:
                return CreateDeleteCommand(batch, row);
        }
        
        return null;
    }

    /// <summary>
    /// Создает команду вставки
    /// </summary>
    private NpgsqlBatchCommand CreateInsertCommand(NpgsqlBatch batch, IRow row)
    {
        var text = GetInsertCommandText();
        var command = batch.CreateBatchCommand();
        command.CommandText = text;
        
        FillWithParameters(command, row, TableSchema.MutableColumns);
        
        return command;
    }

    /// <summary>
    /// Создает команду обновления
    /// </summary>
    private NpgsqlBatchCommand? CreateUpdateCommand(NpgsqlBatch batch, IRow row)
    {
        var modifications = GetModifications(row, TableSchema.NonPrimaryKeyColumns).Select(x => x.Key).ToArray();
        if (modifications.Length == 0)
            return null;
        
        var text = GetUpdateCommandText(modifications);
        var command = batch.CreateBatchCommand();
        command.CommandText = text;
        
        FillWithParameters(command, row, TableSchema.PrimaryKeyColumns);
        FillWithParameters(command, row, modifications);
        AppendFilterParameters(command, _filter);
        
        return command;
    }

    /// <summary>
    /// Создает команду вставки или обновления
    /// </summary>
    private NpgsqlBatchCommand CreateInsertOrUpdateCommand(NpgsqlBatch batch, IRow row)
    {
        var text = GetInsertOrUpdateCommandText();
        var command = batch.CreateBatchCommand();
        command.CommandText = text;
        
        FillWithParameters(command, row, TableSchema.PrimaryKeyColumns);
        FillWithParameters(command, row, TableSchema.NonPrimaryKeyColumns);
        AppendFilterParameters(command, _insertOrUpdateFilter);
        
        return command;
    }

    /// <summary>
    /// Создает команду удаления
    /// </summary>
    private NpgsqlBatchCommand CreateDeleteCommand(NpgsqlBatch batch, IRow row)
    {
        var text = GetDeleteCommandText();
        var command = batch.CreateBatchCommand();
        command.CommandText = text;
        
        FillWithParameters(command, row, TableSchema.PrimaryKeyColumns);
        AppendFilterParameters(command, _filter);
        
        return command;
    }

    /// <summary>
    /// Наполнение команды параметрами
    /// </summary>
    private void FillWithParameters(NpgsqlBatchCommand command, IRow row, IEnumerable<IColumn> columns)
    {
        // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
        foreach (PgColumn column in columns)
        {
            var parameter = command.CreateParameter();
            
            parameter.ParameterName = TableSchema.ColumnParameterMappings[column];
            
            parameter.DataTypeName = column.DataTypeName;

            row.TryGetValue(column.Key, out var value);
            
            parameter.Value = value ?? DBNull.Value;
            
            command.Parameters.Add(parameter);
        }
    }

    /// <summary>
    /// Добавляет параметры из фильтра
    /// </summary>
    private void AppendFilterParameters(NpgsqlBatchCommand command, CommandFilter<NpgsqlParameter>? filter)
    {
        if (filter == null)
            return;

        foreach (var parameter in filter.Parameters)
        {
            command.Parameters.Add(parameter());
        }
    }

    #endregion
    
    #region Тексты команд

    private string? _insertCommandText;
    private string? _deleteCommandText;
    private string? _insertOrUpdateCommandText;

    private string? _whereText;
    private string? _primaryKeyText;

    /// <summary>
    /// Возвращает текст команды на вставку
    /// </summary>
    private string GetInsertCommandText() => LazyInitializer.EnsureInitialized(ref _insertCommandText, () =>
    {
        var names = string.Join(", ", TableSchema.MutableColumns.Select(x => x.Path));
        var parameters = string.Join(", ", TableSchema.MutableColumns.Select(x => TableSchema.ColumnParameterMappings[x]));

        return $"INSERT INTO {TableSchema.Table.Path} ({names}) VALUES ({parameters})";
    });
    
    /// <summary>
    /// Возвращает текст команды на обновление
    /// </summary>
    private string GetUpdateCommandText(IColumn[] modifiedColumns)
    {
        var set = string.Join(", ", modifiedColumns.Select(x => $"{x.Path} = {TableSchema.ColumnParameterMappings[x]}"));
        return $"UPDATE {TableSchema.Table.Path} SET {set} WHERE {GetWhereText()}";
    }
    
    /// <summary>
    /// Возвращает текст команды вставку или обновление
    /// </summary>
    private string GetInsertOrUpdateCommandText() => LazyInitializer.EnsureInitialized(ref _insertOrUpdateCommandText, () =>
    {
        var set = string.Join(", ", TableSchema.NonPrimaryKeyColumns.Select(x => $"{x.Path} = {TableSchema.ColumnParameterMappings[x]}"));
        var text = $"{GetInsertCommandText()} ON CONFLICT ({GetPrimaryKeyText()}) DO UPDATE SET {set}";
        
        if (_insertOrUpdateFilter != null)
            text += $" WHERE {_insertOrUpdateFilter.CommandFilterText}";
        
        return text;
    });

    /// <summary>
    /// Возвращает текст команды на удаление
    /// </summary>
    private string GetDeleteCommandText() =>
        LazyInitializer.EnsureInitialized(ref _deleteCommandText, () => $"DELETE FROM {TableSchema.Table.Path} WHERE {GetWhereText()}");

    /// <summary>
    /// Возвращает текст Where
    /// </summary>
    private string GetWhereText() => LazyInitializer.EnsureInitialized(ref _whereText,
        () =>
        {
            var conditions =
                TableSchema.PrimaryKeyColumns.Select(x => $"{x.Path} = {TableSchema.ColumnParameterMappings[x]}");

            if (_filter != null)
                conditions = conditions.Concat(Enumerable.Repeat($"({_filter.CommandFilterText})", 1));

            return string.Join(" AND ", conditions);
        });

    /// <summary>
    /// Возвращает текст Primary key
    /// </summary>
    private string GetPrimaryKeyText() => LazyInitializer.EnsureInitialized(ref _primaryKeyText,
        () => string.Join(", ", TableSchema.PrimaryKeyColumns.Select(x => x.Path)));

    #endregion

    #region Валидация

    /// <summary>
    /// Валидация после исполнения
    /// </summary>
    protected override void ValidateCount(int count)
    {
        if (_filter != null)
            return;
        
        base.ValidateCount(count);
    }

    #endregion
}