namespace iCUE_ReverseEngineer.Icue.Data;

public class IcueGameMessage
{
    public string Method { get; set; } = string.Empty;
    
    public IcueGameMessageParams? Params { get; set; }
}

public class IcueGameMessageParams
{
    public string? Name { get; set; }
    public string? GameSdkProtocolVersion { get; set; }
    public int? DeviceIndex { get; set; }
}
