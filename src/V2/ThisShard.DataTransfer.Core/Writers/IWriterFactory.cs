using ThisShard.Database.Models.Tables;

namespace ThisShard.Database.Writers;

/// <summary>
/// Фабрика райтеров
/// </summary>
public interface IWriterFactory
{
    /// <summary>
    /// Создает райтера используя таблицу
    /// </summary>
    ValueTask<IWriter> Create(ITable table);
    
    /// <summary>
    /// Создает райтера используя временную таблицу
    /// </summary>
    ValueTask<IWriter> Create(IStagingTable table);
}