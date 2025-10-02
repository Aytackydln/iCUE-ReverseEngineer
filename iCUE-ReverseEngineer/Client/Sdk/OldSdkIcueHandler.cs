namespace iCUE_ReverseEngineer.Client.Sdk;

internal enum OldSdkConnectionState
{
    Handshake,
    DeviceCount,
    DeviceInfo,
    Initialized,
    Unknown,
}

internal class OldSdkIcueHandler : IIcueHandler
{
    private readonly GameClientConnection _connection;
    internal List<GsiGameHandle> GameHandles { get; }

    private OldSdkConnectionState _state = OldSdkConnectionState.Handshake;

    private readonly TaskCompletionSource _started = new();
    private int _deviceCount;
    private int _currentDeviceIndex;

    internal OldSdkIcueHandler(GameClientConnection connection)
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
    
    private async Task AcquireAccessMode(string message)
    {
        const string json = """{"method":"InternalAquireAccessMode","params":{"accessMode":0}}""";
        await _connection.SendMessage(json);
        _started.SetResult();
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
    
    private async Task ReceiveResult(string message)
    {
        switch (_state)
        {
            case OldSdkConnectionState.Handshake:
                
                const string dcJson = """{"method":"CorsairGetDeviceCount","params":{}}""";
                await _connection.SendMessage(dcJson);
                _state = OldSdkConnectionState.DeviceCount;

                break;
            case OldSdkConnectionState.DeviceCount:
                _deviceCount = 2; // TODO This should be dynamic based on the actual device count

                if (_currentDeviceIndex < _deviceCount)
                {
                    var diJson = """{"method":"CorsairGetDeviceInfo","params":{"deviceIndex":""" + _currentDeviceIndex++ + "}}";
                    await _connection.SendMessage(diJson);
                }
                if (_currentDeviceIndex == _deviceCount)
                {
                    _currentDeviceIndex = 0;
                    _state = OldSdkConnectionState.DeviceInfo;
                }
                break;
            case OldSdkConnectionState.DeviceInfo:
                if (_currentDeviceIndex < _deviceCount)
                {
                    var json1 = """{"method":"CorsairGetLedPositionsByDeviceIndex","params":{"deviceIndex":""" + _currentDeviceIndex++ + "}}";
                    await _connection.SendMessage(json1);
                }
                if (_currentDeviceIndex == _deviceCount)
                {
                    _currentDeviceIndex = 0;
                    _state = OldSdkConnectionState.Initialized;
                }
                break;
            default:
                Console.WriteLine("[SdkIcueHandler] Unhandled state");
                break;
        }
    }
}