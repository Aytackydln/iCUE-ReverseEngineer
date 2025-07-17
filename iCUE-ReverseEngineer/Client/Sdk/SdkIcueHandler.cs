namespace iCUE_ReverseEngineer.Client.Sdk;

internal enum SdkConnectionState
{
    Handshake,
    DeviceCount,
    DeviceInfo,
    Initialized,
}

public class SdkIcueHandler : IIcueHandler
{
    private readonly GameClientConnection _connection;
    public List<GsiGameHandle> GameHandles { get; }

    private SdkConnectionState _state = SdkConnectionState.Handshake;
    private int _currentDeviceIndex;
    private bool[] _deviceInfoReceived = [];

    public SdkIcueHandler(GameClientConnection connection)
    {
        _connection = connection;
        GameHandles =
        [
            new GsiGameHandle(
                isMatch: message => message.Contains("\"breakingChanges\":false"),
                doHandle: AskDeviceCount
            ),
            new GsiGameHandle(
                isMatch: message => message.StartsWith("{\"result\":"),
                doHandle: ReceiveResult
            ),
        ];

        connection.MessageReceived += GameOutReceived;
        connection.CallbackReceived += GameCallbackReceived;
    }

    public async Task Start()
    {
        var json = """{"method":"CorsiarHandshakeMethod","params":{"sdkProtocolVersion":10}}""";
        await _connection.SendMessage(json);
    }

    public async void GameOutReceived(object? sender, string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[GameOut]:\n{message}");
        Console.ResetColor();

        var handle = GameHandles.FirstOrDefault(handle => handle.IsMatch(message));
        if (handle == null)
        {
            Console.WriteLine($"[GameOut] unhandled message: {message}");
            return;
        }

        await handle.DoHandle(message);
    }

    public void GameCallbackReceived(object? sender, string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.BackgroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine($"[GameCallback]:\n{message}");
        Console.ResetColor();
    }

    private async Task AskDeviceCount(string message)
    {
        if (_state != SdkConnectionState.Handshake)
        {
            Console.WriteLine("[SdkIcueHandler] AskDeviceCount called in wrong state.");
            return;
        }

        _state = SdkConnectionState.DeviceCount;

        const string json = """{"method":"CorsairGetDeviceCount"}""";
        await _connection.SendMessage(json);
    }

    private async Task ReceiveResult(string message)
    {
        switch (_state)
        {
            case SdkConnectionState.DeviceCount:
                _state = SdkConnectionState.DeviceInfo;

                //TODO handle device count
                var deviceCount = 2; // This should be dynamic based on the actual device count
                _deviceInfoReceived = new bool[deviceCount];

                const string json1 = """{"method":"CorsairGetDeviceInfo","params":{"deviceIndex":0}}""";
                await _connection.SendMessage(json1);
                const string json2 = """{"method":"CorsairGetDeviceInfo","params":{"deviceIndex":1}}""";
                await _connection.SendMessage(json2);

                break;
            case SdkConnectionState.DeviceInfo:
                _deviceInfoReceived[_currentDeviceIndex] = true;
                _currentDeviceIndex++;

                if (_currentDeviceIndex >= _deviceInfoReceived.Length)
                {
                    // All device info received, reset state
                    _state = SdkConnectionState.Initialized;
                    Console.WriteLine("[SdkIcueHandler] All device info received.");

                    const string getDevicePositions = """{"method":"CorsairGetLedPositions"}""";
                    await _connection.SendMessage(getDevicePositions);

                    const string setLedColors = """{"method":"CorsairSetLedsColors","params":{"ledsColors":"1,255,0,0#2,255,0,0#3,255,0,0#4,255,0,0#5,255,0,0#6,255,0,0#7,255,0,0#8,255,0,0"}}""";
                    await _connection.SendMessage(setLedColors);
                }

                break;
            case SdkConnectionState.Initialized:

                const string ledColors = """
                                         {"method":"CorsairSetLedsColors","params":{"ledsColors":"1,255,0,0#2,255,0,0#3,255,0,0#4,255,0,0#5,255,0,0#6,255,0,0#7,255,0,0#8,255,0,0#9,255,0,0#10,255,0,0#11,255,0,0#12,255,0,0#13,255,0,0#14,255,0,0#15,255,0,0#16,255,0,0#17,255,0,0#18,255,0,0#19,255,0,0#20,255,0,0#21,255,0,0#22,255,0,0#23,255,0,0#24,255,0,0#25,255,0,0#26,255,0,0#27,255,0,0#28,255,0,0#29,255,0,0#30,255,0,0#31,255,0,0#32,255,0,0#33,255,0,0#34,255,0,0#35,255,0,0#36,255,0,0#37,255,0,0#38,255,0,0#39,255,0,0#40,255,0,0#41,255,0,0#42,255,0,0#43,255,0,0#44,255,0,0#45,255,0,0#46,255,0,0#47,255,0,0#48,255,0,0#49,255,0,0#50,255,0,0#51,255,0,0#52,255,0,0#53,255,0,0#54,255,0,0#55,255,0,0#56,255,0,0#57,255,0,0#58,255,0,0#59,255,0,0#60,255,0,0#61,255,0,0#62,255,0,0#63,255,0,0#64,255,0,0#65,255,0,0#66,255,0,0#67,255,0,0#68,255,0,0#69,255,0,0#70,255,0,0#71,255,0,0#72,255,0,0#73,255,0,0#74,255,0,0#75,255,0,0#76,255,0,0#77,255,0 ,0#78,255,0,0#79,255,0,0#80,255,0,0#81,255,0,0#82,255,0,0#83,255,0,0#84,255,0,0#85,255,0,0#86,255,0,0#87,255,0,0#88,255,0,0#89,255,0,0#90,255,0,0#91,255,0,0#92,255,0,0#93,255,0,0#94,255,0,0#95,255,0,0#96,255,0,0#97,255,0,0# 98,255,0,0#99,255,0,0#100,255,0,0#101,255,0,0#102,255,0,0#103,255,0,0#104,255,0,0#105,255,0,0#106,255,0,0#107,255,0,0#108,255,0,0#109,255,0,0#110,255,0,0#111,255,0,0#112,255,0,0#113,255,0,0#114,255,0,0#115,255,0,0#116,255,0 ,0#117,255,0,0#118,255,0,0#119,255,0,0#120,255,0,0#121,255,0,0#122,255,0,0#123,255,0,0#124,255,0,0#125,255,0,0#126,255,0,0#127,255,0,0#128,255,0,0#129,255,0,0#130,255,0,0#131,255,0,0#132,255,0,0#133,255,0,0#134,255,0,0#135,255,0,0#136,255,0,0#137,255,0,0#138,255,0,0#139,255,0,0#140,255,0,0#141,255,0,0#142,255,0,0#143,255,0,0#144,255,0,0#145,255,0,0#146,255,0,0#147,255,0,0#154,255,0,0"}}
                                         """;
                await _connection.SendMessage(ledColors);
                break;
            default:
                Console.WriteLine("[SdkIcueHandler] Unhandled state");
                break;
        }
    }
}