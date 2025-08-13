using System.Collections.Frozen;
using System.Text.Json;
using iCUE_ReverseEngineer.Icue.Data;

namespace iCUE_ReverseEngineer.Icue.Sdk;

public class SdkHandler
{
    private static readonly int MaxKeyId = Enum.GetValues<IcueLedId>().Cast<int>().Max() + 1;

    public event EventHandler? ColorsUpdated;
    public event EventHandler? GameConnected;
    internal FrozenDictionary<string, Action<IcueGameMessage>> SdkHandles { get; }
    public Dictionary<IcueLedId, IcueColor> LedColors { get; } = new(MaxKeyId);

    private readonly IcueToGameConnection _gameConnection;

    internal SdkHandler(IcueToGameConnection gameConnection)
    {
        _gameConnection = gameConnection;

        SdkHandles = new Dictionary<string, Action<IcueGameMessage>>
        {
            { "CorsiarHandshakeMethod", Handshake },
            { "CorsairGetDeviceCount", DeviceCount },
            { "CorsairGetDeviceInfo", DeviceInfo },
            { "CorsairGetLedPositions", LedPositions },
            { "CorsairSetLedsColors", SetLedsColors },
        }.ToFrozenDictionary();
    }

    private void Handshake(IcueGameMessage message)
    {
        const string handshakeOkay = """{"serverProtocolVersion":16,"serverVersion":"5.30.90","breakingChanges":false}""";
        _gameConnection.SendGameMessage(handshakeOkay);
        GameConnected?.Invoke(this, EventArgs.Empty);
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
        var ledPositionsResponse = """{"result":""" + JsonSerializer.Serialize<IcueLed[]>(DevicesPreset.LedPositions, IcueJsonContext.Default.IcueLedArray) +
                                   "}";
        _gameConnection.SendGameMessage(ledPositionsResponse);
    }

    private void SetLedsColors(IcueGameMessage message)
    {
        if (message.Params?.LedsColors == null || message.Params.LedsColors.Length == 0)
        {
            RespondOk(message);
            return;
        }

        var ledsString = message.Params.LedsColors;
        // iterate trough LEDs, 0 alloc
        IterateLedsColors(ledsString);
        ColorsUpdated?.Invoke(this, EventArgs.Empty);

        // Here you would handle the LED colors, for now we just respond OK
        RespondOk(message);
    }

    private void IterateLedsColors(string ledsColors)
    {
        var span = ledsColors.AsSpan();
        var currentLedIdx = 0;
        while (currentLedIdx < span.Length)
        {
            var currentLedEndIdx = span[currentLedIdx..].IndexOf('#');
            if (currentLedEndIdx == -1) currentLedEndIdx = span.Length - currentLedIdx;

            var ledEntry = span.Slice(currentLedIdx, currentLedEndIdx);

            var ledColor = LedsStringParser.ParseLedColor(ledEntry);
            LedColors[ledColor.LedId] = ledColor.ToIcueColor();

            currentLedIdx += currentLedEndIdx + 1;
        }
    }

    private void RespondOk(IcueGameMessage message)
    {
        const string setGameResponse = """{"result":true,"errorCode":0}""";
        _gameConnection.SendGameMessage(setGameResponse);
    }
}