using ThisShard.Database.Core.Mappers;

namespace ThisShard.Database.Core.Models.Rows;

/// <summary>
/// Смапленная строка использующая маппер
/// </summary>
public class MappedRow : IRow
{
    private readonly IRow _row;
    private readonly IRowMapper _mapper;

    /// <summary>
    /// Состояние
    /// </summary>
    public RowState State => _mapper.GetRowState(_row);

    /// <summary>
    /// Возвращает список ключей
    /// </summary>
    public IEnumerable<string> GetKeys() => _row.GetKeys();

    /// <summary>
    /// Метаданные строки
    /// </summary>
    public IDictionary<string, object?> Metadata => _row.Metadata;

    public MappedRow(IRow row, IRowMapper mapper)
    {
        _row = row ?? throw new ArgumentNullException(nameof(row));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// Пытается получить значение ячейки
    /// </summary>
    public bool TryGetValue(string columnKey, out object? value) => 
        _mapper.TryGetValue(_row, columnKey, out value);
}