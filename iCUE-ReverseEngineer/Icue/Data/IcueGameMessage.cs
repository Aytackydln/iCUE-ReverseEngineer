using System.Text.Json.Serialization;
using iCUE_ReverseEngineer.Util;

namespace iCUE_ReverseEngineer.Icue.Data;

public class IcueGameMessage
{
    public string Method { get; set; } = string.Empty;
    
    public IcueGameMessageParams? Params { get; set; }
}

public class IcueGameMessageParams
{
    public string? Name { get; set; }
    
    // allow this to be parsed from numbers or strings
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? GameSdkProtocolVersion { get; set; }
    public int? DeviceIndex { get; set; }
    
    /// <summary>
    /// # separated list of LED colors in , separated LED ID, R, G, B format.
    /// Example: "1,255,0,0#2,0,255,0#3,0,0,255"
    /// </summary>
    public string? LedsColors { get; set; }
}
