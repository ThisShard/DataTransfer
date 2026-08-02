namespace ThisShard.Database.Core.Converters.Handlers;

public interface IValueConverterHandler
{
    Type? SourceType { get; }
    
    Type? TargetType { get; }
    
    bool CanConvert(Type sourceType, Type targetType);
    
    object? Convert(object? value, Type sourceType, Type targetType);
}