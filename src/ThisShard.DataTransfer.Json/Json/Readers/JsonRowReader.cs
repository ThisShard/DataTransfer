using System.Text.Json;
using System.Text.Json.Stream;
using ThisShard.Database.Core.Models;
using ThisShard.Database.Core.Models.Rows;
using ThisShard.Database.Core.Readers;
using ThisShard.Database.Infrastructure.Json.Helpers;

namespace ThisShard.Database.Infrastructure.Json.Readers;

public class JsonRowReader : IRowReader, IDisposable
{
    private readonly IUtf8JsonAsyncStreamReader _jsonReader;
    private readonly bool _ownsReader;
    private readonly RowState _defaultRowState;
    private readonly string? _rowStatePropertyName;
    private readonly Func<string, string>? _propertyNameResolver;
    
    private bool _isStarted;
    private bool _isFinished;

    public JsonRowReader(IUtf8JsonAsyncStreamReader jsonReader, RowState defaultRowState = RowState.Ignored, Func<string, string>? propertyNameResolver = null, string? rowStatePropertyName = null, bool ownsReader = true)
    {
        _jsonReader = jsonReader ?? throw new ArgumentNullException(nameof(jsonReader));
        _ownsReader = ownsReader;
        _defaultRowState = defaultRowState;
        _propertyNameResolver = propertyNameResolver;
        _rowStatePropertyName = rowStatePropertyName;
    }

    /// <summary>
    /// Читает следующую строку
    /// </summary>
    public async ValueTask<IRow?> Read()
    {
        if (_isFinished)
            return null;
        
        if (!await _jsonReader.ReadAsync())
            return null;
        
        //Пропускаем токен начала массива
        if (_jsonReader.TokenType == JsonTokenType.StartArray)
        {
            if (_isStarted)
                throw new InvalidOperationException($"Invalid token {_jsonReader.TokenType}. Token must be {JsonTokenType.StartArray}.");
            
            if (!await _jsonReader.ReadAsync())
                throw new InvalidOperationException($"Unexpected end of file.");
            
            _isStarted = true;
        }

        //Если встретили токен конца массива - заканчиваем чтение
        if (_jsonReader.TokenType == JsonTokenType.EndArray)
        {
            _isFinished = true;
            return null;
        }
        
        //Если встретили токен
        if (_jsonReader.TokenType != JsonTokenType.StartObject)
            throw new InvalidOperationException($"Invalid token {_jsonReader.TokenType}. Token must be {JsonTokenType.StartObject}.");
        
        var data = await _jsonReader.DeserializeAsync<Dictionary<string, object?>>();
        if (data == null)
            throw new InvalidOperationException($"Unexpected end of file.");

        //Получаем состояние строки
        var state = GetRowState(data);

        data = data.ToDictionary(
            x => _propertyNameResolver != null ? _propertyNameResolver(x.Key) : x.Key,
            x => JsonReaderHelper.GetValue(x.Value)
            );
        
        return new Row()
        {
            Data = data,
            State = state,
        };
    }

    /// <summary>
    /// Возвращает состояние строки из данных
    /// </summary>
    private RowState GetRowState(Dictionary<string, object?> data)
    {
        var state = _defaultRowState;
        
        if (_rowStatePropertyName != null && data.TryGetValue(_rowStatePropertyName, out var rowStateObj) && rowStateObj is string rowStateValue && Enum.TryParse(rowStateValue, true, out RowState s))
        {
            state = s;
            data.Remove(_rowStatePropertyName);
        }

        return state;
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        if (_ownsReader)
            _jsonReader.Dispose();
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.</summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}