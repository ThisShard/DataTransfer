using ThisShard.Database.Core.Models.Tables;
using ThisShard.Database.Infrastructure.Excel.Models;

namespace ThisShard.Database.Infrastructure.Excel.Tables;

/// <summary>
/// Провайдер Excel таблиц
/// </summary>
public interface IExcelTableProvider
{
    /// <summary>
    /// Возвращает таблицу для типа
    /// </summary>
    ExcelTable GetTable(Type type);
    
    /// <summary>
    /// Конвертирует таблицу в Excel
    /// </summary>
    ExcelTable? ConvertTable(ITable table);
}