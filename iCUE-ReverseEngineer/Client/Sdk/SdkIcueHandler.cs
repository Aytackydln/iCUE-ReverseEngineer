namespace iCUE_ReverseEngineer.Client.Sdk;

internal enum SdkConnectionState
{
    Handshake,
    DeviceCount,
    DeviceInfo,
    Initialized,
}

internal class SdkIcueHandler : IIcueHandler
{
    private readonly GameClientConnection _connection;
    internal List<GsiGameHandle> GameHandles { get; }

    private SdkConnectionState _state = SdkConnectionState.Handshake;
    private int _currentDeviceIndex;
    private bool[] _deviceInfoReceived = [];
    
    private readonly TaskCompletionSource _started = new();

    internal SdkIcueHandler(GameClientConnection connection)
    {
        _connection = connection;
        GameHandles =
        [
            new GsiGameHandle(
                isMatch: message => message.Contains("\"breakingChanges\":false"),
                doHandle: AcquireAccessMode
            ),
            new GsiGameHandle(
                isMatch: message => message.Contains("\"result\":"),
                doHandle: ReceiveResult
            ),
        ];

        connection.MessageReceived += GameOutReceived;
        connection.CallbackReceived += GameCallbackReceived;
    }

    async Task IIcueHandler.Start()
    {
        const string json = """{"method":"CorsiarHandshakeMethod","params":{"sdkProtocolVersion":9}}""";
        await _connection.SendMessage(json);
        await _started.Task;
    }

    private async void GameOutReceived(object? sender, string message)
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

    private void GameCallbackReceived(object? sender, string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.BackgroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine($"[GameCallback]:\n{message}");
        Console.ResetColor();
    }

    private async Task AcquireAccessMode(string message)
    {
        const string json = """{"method":"CorsairSetLayerPriority","params":{"mode":0}}""";
        await _connection.SendMessage(json);
    }

    private async Task ReceiveResult(string message)
    {
        switch (_state)
        {
            case SdkConnectionState.Handshake:
                // Handshake response received, now ask for device count
                if (_state != SdkConnectionState.Handshake)
                {
                    Console.WriteLine("[SdkIcueHandler] AskDeviceCount called in wrong state.");
                    return;
                }

                _state = SdkConnectionState.DeviceCount;

                const string json = """{"method":"CorsairGetDeviceCount"}""";
                await _connection.SendMessage(json);

                break;
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
                }

                break;
            case SdkConnectionState.Initialized:
                _started.TrySetResult();
                break;
            default:
                Console.WriteLine("[SdkIcueHandler] Unhandled state");
                break;
        }
    }
}