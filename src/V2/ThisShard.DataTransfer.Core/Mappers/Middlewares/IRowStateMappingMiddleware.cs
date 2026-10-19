using ThisShard.Database.Models.Rows;

namespace ThisShard.Database.Mappers.Middlewares;

/// <summary>
/// Миддлварь мэппинга состояний строк
/// </summary>
public interface IRowStateMappingMiddleware : IRowMappingMiddleware
{
    /// <summary>
    /// Пытается получить смапленное смапленное состояние строки
    /// </summary>
    RowState GetRowState(IRow source, GetRowStateDelegate next);
}

/// <summary>
/// Делегат получения состояния строки
/// </summary>
public delegate RowState GetRowStateDelegate(IRow source);