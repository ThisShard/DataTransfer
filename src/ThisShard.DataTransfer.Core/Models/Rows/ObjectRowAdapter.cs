using System.Linq.Expressions;
using System.Reflection;

namespace ThisShard.Database.Core.Models.Rows;

/// <summary>
/// Адаптер объекта к интерфейсу IRow
/// </summary>
public class ObjectRowAdapter<T> : IRow
{
    #region Static

    private static readonly IReadOnlyDictionary<string, Func<T, object?>> PropertyAccessors;

    static ObjectRowAdapter()
    {
        PropertyAccessors = typeof(T).GetProperties().ToDictionary(x => x.Name, CreateAccessor);
    }
    
    /// <summary>
    /// Создает аксессор к свойству ресурса
    /// </summary>
    private static Func<T, object?> CreateAccessor(PropertyInfo propertyInfo)
    {
        var parameterExpression = Expression.Parameter(typeof(T));
        var callExpression = Expression.Convert(Expression.Property(parameterExpression, propertyInfo), typeof(object));
        var lambdaExpression = Expression.Lambda<Func<T, object?>>(callExpression, parameterExpression);
        return lambdaExpression.Compile();
    }
    
    #endregion

    private IDictionary<string, object?>? _metadata;
    
    /// <summary>
    /// Состояние
    /// </summary>
    public RowState State { get; set; }
    
    /// <summary>
    /// Объект
    /// </summary>
    public T? Object { get; set; }

    /// <summary>
    /// Возвращает список ключей
    /// </summary>
    public IEnumerable<string> GetKeys() => PropertyAccessors.Keys;

    /// <summary>
    /// Метаданные строки
    /// </summary>
    public IDictionary<string, object?> Metadata => _metadata ??= new Dictionary<string, object>()!;

    /// <summary>
    /// Пытается получить значение ячейки
    /// </summary>
    public bool TryGetValue(string columnKey, out object? value)
    {
        value = null;
        if (Object == null)
            return false;

        if (!PropertyAccessors.TryGetValue(columnKey, out var accessor))
            return false;
        
        value = accessor(Object);
        return true;
    }
}