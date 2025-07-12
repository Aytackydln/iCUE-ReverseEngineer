using System.Text.Json;
using System.Text.Json.Serialization;

namespace iCUE_ReverseEngineer.Icue;

public class IcueGameMessage
{
    public string Method { get; set; } = string.Empty;
    
    [JsonExtensionData]
    public IDictionary<string, JsonElement> Params { get; set; } = new Dictionary<string, JsonElement>();
}

[JsonSerializable(typeof(IcueGameMessage))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
public partial class IcueJsonContext : JsonSerializerContext;