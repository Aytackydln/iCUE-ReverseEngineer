using System.Text.Json.Serialization;

namespace iCUE_ReverseEngineer.Icue.Data;

[JsonSerializable(typeof(IcueGameMessage))]
[JsonSerializable(typeof(IcueDevice))]
[JsonSerializable(typeof(IcueLed[]))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = false)]
public partial class IcueJsonContext : JsonSerializerContext;