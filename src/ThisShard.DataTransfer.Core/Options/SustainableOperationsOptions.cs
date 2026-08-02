using System.Data;
using System.Data.Common;
using ThisShard.Database.Core.Extensions;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;

namespace ThisShard.Database.Core.Options;

/// <summary>
/// Настройки устойчивых операций
/// </summary>
public record SustainableOperationsOptions<TConnection>
    where TConnection: DbConnection
{
    /// <summary>
    /// Настройки по умолчанию
    /// </summary>
    public static SustainableOperationsOptions<TConnection> Default { get; set; } = new();
    
    /// <summary>
    /// Отключенные настройки устойчивых операций
    /// </summary>
    public static SustainableOperationsOptions<TConnection> Disabled { get; } = new()
    {
        MaxRetryCount = 0,
        TerminatePredicate = (_,_) => true,
    };
    
    /// <summary>
    /// Максимальное количество повторений
    /// </summary>
    public int MaxRetryCount { get; init; } = -1;

    /// <summary>
    /// Возвращает задержку перед очередной попыткой
    /// </summary>
    public Func<int, int> GetRetryDelay { get; init; } = retry => 0;

    /// <summary>
    /// Признак остановки при попытках повтора
    /// </summary>
    public Func<TConnection, Exception, bool> TerminatePredicate { get; init; } =
        (cn, _) => (cn.State & ConnectionState.Broken) == 0;
    
    /// <summary>
    /// Конвертер строк при повторной попытке обработки
    /// </summary>
    public Func<IRow, IRow?>? RowConverterOnRetry { get; init; } = row => row.State == row.State.GetSafeState()
        ? row
        : new StateRowWrapper(row) { State = row.State.GetSafeState() };
}