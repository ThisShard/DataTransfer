using System.Data;
using Microsoft.Data.Sqlite;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Columns;
using ThisShard.Database.Core.Models.Filters;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Core.Schemas;
using ThisShard.Database.Core.Writers;
using ThisShard.Database.Infrastructure.Sqlite.Models;

namespace ThisShard.Database.Infrastructure.Sqlite.Writers;

/// <summary>
/// Писатель в таблицу Sqlite
/// </summary>
public class SqliteTableWriter : RelationalTableWriter
{
    private static readonly TableSchemaProvider SchemaProvider = new((_, i) => $"$col_{i}");
    
    private readonly SqliteConnection _connection;
    private readonly Func<CommandFilterFactoryContext, CommandFilter<SqliteParameter>?>? _filterFactory;

    private CommandFilter<SqliteParameter>? _filter;
    private CommandFilter<SqliteParameter>? _insertOrUpdateFilter;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SqliteTableWriter(
        SqliteConnection connection, 
        int bufferSize, 
        Func<CommandFilterFactoryContext, CommandFilter<SqliteParameter>?>? filterFactory = null
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
        var sqliteTable = table as SqliteTable;
        if (sqliteTable == null)
            throw new ArgumentOutOfRangeException(nameof(table));
        
        TableSchema = SchemaProvider.GetSchema(sqliteTable);

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
        
        foreach (var row in Buffer)
        {
            await using var command = CreateCommand(row);
            if (command == null)
                continue;
            
            var count = await command.ExecuteNonQueryAsync();
            if (_filter == null && ShouldValidateCount(row.State) && count <= 0)
                throw new DBConcurrencyException();
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
    /// Дополняет команду строкой
    /// </summary>
    private SqliteCommand? CreateCommand(IRow row)
    {
        switch (row.State)
        {
            case RowState.AddedOrModified when TableSchema is { CanInsert: true, CanUpdate: true }:
                return CreateInsertOrUpdateCommand(row);
            case RowState.AddedOrModified when TableSchema.CanInsert:
            case RowState.Added:
                return CreateInsertCommand(row);
            case RowState.AddedOrModified when TableSchema.CanUpdate:
            case RowState.Modified:
                return CreateUpdateCommand(row);
            case RowState.Deleted:
            case RowState.SafeDeleted:
                return CreateDeleteCommand(row);
        }
        
        return null;
    }

    /// <summary>
    /// Создает команду вставкой
    /// </summary>
    private SqliteCommand CreateInsertCommand(IRow row)
    {
        var text = GetInsertCommandText();
        var command = _connection.CreateCommand();
        command.CommandText = text;
        
        FillWithParameters(command, row, TableSchema.MutableColumns);
        
        return command;
    }

    /// <summary>
    /// Создает команду обновления
    /// </summary>
    private SqliteCommand? CreateUpdateCommand(IRow row)
    {
        var modifications = GetModifications(row, TableSchema.NonPrimaryKeyColumns).Select(x => x.Key).ToArray();
        if (modifications.Length == 0)
            return null;
        
        var text = GetUpdateCommandText(modifications);
        var command = _connection.CreateCommand();
        command.CommandText = text;
        
        FillWithParameters(command, row, TableSchema.PrimaryKeyColumns);
        FillWithParameters(command, row, modifications);
        AppendFilterParameters(command, _filter);
        
        return command;
    }

    /// <summary>
    /// Создает команду вставки или обновления
    /// </summary>
    private SqliteCommand CreateInsertOrUpdateCommand(IRow row)
    {
        var text = GetInsertOrUpdateCommandText();
        var command = _connection.CreateCommand();
        command.CommandText = text;
        
        FillWithParameters(command, row, TableSchema.PrimaryKeyColumns);
        FillWithParameters(command, row, TableSchema.NonPrimaryKeyColumns);
        AppendFilterParameters(command, _insertOrUpdateFilter);
        
        return command;
    }

    /// <summary>
    /// Создает команду удаления
    /// </summary>
    private SqliteCommand CreateDeleteCommand(IRow row)
    {
        var text = GetDeleteCommandText();
        var command = _connection.CreateCommand();
        command.CommandText = text;
        
        FillWithParameters(command, row, TableSchema.PrimaryKeyColumns);
        AppendFilterParameters(command, _filter);
        
        return command;
    }

    /// <summary>
    /// Наполнение команды параметрами
    /// </summary>
    private void FillWithParameters(SqliteCommand command, IRow row, IEnumerable<IColumn> columns)
    {
        // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
        foreach (SqliteColumn column in columns)
        {
            var parameter = command.CreateParameter();
            
            parameter.ParameterName = TableSchema.ColumnParameterMappings[column];
            
            row.TryGetValue(column.Key, out var value);
            
            parameter.Value = value ?? DBNull.Value;
            
            command.Parameters.Add(parameter);
        }
    }

    /// <summary>
    /// Добавляет параметры из фильтра
    /// </summary>
    private void AppendFilterParameters(SqliteCommand command, CommandFilter<SqliteParameter>? filter)
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
    
}