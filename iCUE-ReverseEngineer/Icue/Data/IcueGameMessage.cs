using System.Text.Json.Serialization;
using iCUE_ReverseEngineer.Util;

namespace iCUE_ReverseEngineer.Icue.Data;

internal class IcueGameMessage
{
    internal string Method { get; set; } = string.Empty;
    
    internal IcueGameMessageParams? Params { get; set; }
}

internal class IcueGameMessageParams
{
    internal string? Name { get; set; }
    
    // allow this to be parsed from numbers or strings
    [JsonConverter(typeof(StringOrNumberConverter))]
    internal string? GameSdkProtocolVersion { get; set; }
    internal int? DeviceIndex { get; set; }
    
    /// <summary>
    /// # separated list of LED colors in , separated LED ID, R, G, B format.
    /// Example: "1,255,0,0#2,0,255,0#3,0,0,255"
    /// </summary>
    internal string? LedsColors { get; set; }
}
