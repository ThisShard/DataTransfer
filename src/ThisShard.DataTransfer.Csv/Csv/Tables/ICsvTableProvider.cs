using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Infrastructure.Csv.Models;

namespace ThisShard.Database.Infrastructure.Csv.Tables;

/// <summary>
/// Провайдер Csv таблиц
/// </summary>
public interface ICsvTableProvider
{
    /// <summary>
    /// Возвращает таблицу для типа
    /// </summary>
    CsvTable GetTable(Type type);
    
    /// <summary>
    /// Конвертирует таблицу в Csv
    /// </summary>
    CsvTable? ConvertTable(ITable table);
}