using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaWatcher;

public sealed class JsonLineWriter
{
    private readonly object outputLock = new();

    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public void Write<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, jsonOptions);
        lock (outputLock)
        {
            Console.Out.WriteLine(json);
            Console.Out.Flush();
        }
    }
}
