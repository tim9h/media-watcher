using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBridge.IO;

public sealed class JsonLineWriter
{
    private readonly Lock sync = new();

    private readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public void Write<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, options);

        lock (sync)
        {
            Console.WriteLine(json);
            Console.Out.Flush();
        }
    }
}
