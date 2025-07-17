using System.Text.Json;
using iCUE_ReverseEngineer.Icue.Data;

namespace iCUE_ReverseEngineer.Icue.Sdk;

public class SdkHandler
{
    public Dictionary<string, Action<IcueGameMessage>> SdkHandles { get; }

    private readonly IcueToGameConnection _gameConnection;

    public SdkHandler(IcueToGameConnection gameConnection)
    {
        _gameConnection = gameConnection;

        SdkHandles = new Dictionary<string, Action<IcueGameMessage>>
        {
            { "CorsiarHandshakeMethod", Handshake },
            { "CorsairGetDeviceCount", DeviceCount },
            { "CorsairGetDeviceInfo", DeviceInfo },
            { "CorsairGetLedPositions", LedPositions },
            { "CorsairSetLedsColors", RespondOk },
        };
    }

    private void Handshake(IcueGameMessage message)
    {
        const string handshakeOkay = """{"serverProtocolVersion":16,"serverVersion":"5.30.90","breakingChanges":false}""";
        _gameConnection.SendGameMessage(handshakeOkay);
    }

    private void DeviceCount(IcueGameMessage obj)
    {
        var deviceCount = DevicesPreset.Devices.Length;
        var deviceCountResponse = """{"result":""" + deviceCount + "}";
        _gameConnection.SendGameMessage(deviceCountResponse);
    }

    private void DeviceInfo(IcueGameMessage message)
    {
        var deviceIndex = message.Params?.DeviceIndex ?? 0;
        var device = DevicesPreset.Devices[deviceIndex];
        var deviceJson = JsonSerializer.Serialize(device, IcueJsonContext.Default.IcueDevice);
        var result = """{"result":""" + deviceJson + "}";
        _gameConnection.SendGameMessage(result);
    }

    private void LedPositions(IcueGameMessage obj)
    {
        var ledPositionsResponse = """{"result":""" + JsonSerializer.Serialize<IcueLed[]>(DevicesPreset.LedPositions, IcueJsonContext.Default.IcueLedArray) + "}";
        _gameConnection.SendGameMessage(ledPositionsResponse);
    }

    private void RespondOk(IcueGameMessage message)
    {
        const string setGameResponse = """{"result":true,"errorCode":0}""";
        _gameConnection.SendGameMessage(setGameResponse);
    }
}