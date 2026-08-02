using System.Linq.Expressions;
using System.Reflection;

namespace ThisShard.Database.Core.Models.Rows;

/// <summary>
/// Адаптер объекта к интерфейсу IRow
/// </summary>
public class ObjectRowAdapter<T> : IRow
{
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

    /// <summary>
    /// Состояние
    /// </summary>
    public RowState State { get; set; }
    
    /// <summary>
    /// Объект
    /// </summary>
    public T? Object { get; set; }

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