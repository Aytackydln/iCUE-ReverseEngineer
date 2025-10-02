using System.Collections.Frozen;
using System.Text.Json;
using iCUE_ReverseEngineer.Icue.Data;

namespace iCUE_ReverseEngineer.Icue.Sdk;

public class SdkHandler
{
    private static readonly int MaxKeyId = Enum.GetValues<IcueLedId>().Cast<int>().Max() + 1;

    public event EventHandler? GameConnected;
    public event EventHandler? ColorsUpdated;
    internal FrozenDictionary<string, Action<IcueGameMessage>> SdkHandles { get; }
    public Dictionary<IcueLedId, IcueColor> LedColors { get; } = new(MaxKeyId);

    private readonly IcueToGameConnection _gameConnection;

    internal SdkHandler(IcueToGameConnection gameConnection)
    {
        _gameConnection = gameConnection;

        SdkHandles = new Dictionary<string, Action<IcueGameMessage>>
        {
            // old SDK methods
            { "InternalAquireAccessMode" , RespondOk},
            { "CorsiarReleaseControlMethod", RespondOk },
            { "CorsairSetLayerPriority", RespondOk },
            { "CorsairSubscribeForEventsMethod", RespondOk },
            
            // new SDK methods
            { "CorsiarHandshakeMethod", Handshake },
            { "CorsairGetDeviceCount", DeviceCount },
            { "CorsairGetDeviceInfo", DeviceInfo },
            { "CorsairGetLedPositions", LedPositions },
            { "CorsairGetLedPositionsByDeviceIndex" , LedPositionsByDeviceIndex},
            { "CorsairSetLedsColors", SetLedsColors },
            { "CorsairSetLedsColorsFlushBuffer", SetLedsColorsFlushBuffer },
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
        var ledPositionsJson = JsonSerializer.Serialize<IcueLed[]>(DevicesPreset.KeyboardLedPositions, IcueJsonContext.Default.IcueLedArray);
        var ledPositionsResponse = $$"""{"result":{{ledPositionsJson}}}""";
        _gameConnection.SendGameMessage(ledPositionsResponse);
    }
    
    private void LedPositionsByDeviceIndex(IcueGameMessage message)
    {
        var deviceIndex = message.Params?.DeviceIndex ?? 0;
        var deviceInfo = DevicesPreset.Devices[deviceIndex];
        
        // dunno why, but iCUE returns errorCode 5 if PhysicalLayout is 0
        if (deviceInfo.PhysicalLayout == 0)
        {
            const string errorResponse = """{"errorCode":5}""";
            _gameConnection.SendGameMessage(errorResponse);
            return;
        }
        
        var deviceLeds = DevicesPreset.LedPositionsByDevice[deviceIndex];
        
        var ledsJson = JsonSerializer.Serialize<IcueLed[]>(deviceLeds, IcueJsonContext.Default.IcueLedArray);
        var ledPositionsResponse = $$"""{"result":{{ledsJson}}}""";
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

    private void SetLedsColorsFlushBuffer(IcueGameMessage message)
    {
        if (message.Params?.LedsColorsByDeviceIndex == null)
        {
            RespondOk(message);
            return;
        }

        foreach (var ledsString in message.Params.LedsColorsByDeviceIndex.Select(lc => lc.LedColors))
        {
            IterateLedsColors(ledsString);
        }

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