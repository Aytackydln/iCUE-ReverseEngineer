using System.Text.Json.Serialization;

namespace iCUE_ReverseEngineer.Icue;

public class IcueGameMessage
{
    public string Method { get; set; } = string.Empty;
    
    public IcueGameMessageParams? Params { get; set; }
}

public class IcueGameMessageParams
{
    public string? Name { get; set; }
    public string? GameSdkProtocolVersion { get; set; }
    public string? DeviceIndex { get; set; }
}

[JsonSerializable(typeof(IcueGameMessage))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
public partial class IcueJsonContext : JsonSerializerContext;