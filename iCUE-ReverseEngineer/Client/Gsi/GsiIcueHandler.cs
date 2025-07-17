namespace iCUE_ReverseEngineer.Client.Game;

public class GsiIcueHandler : IIcueHandler
{
    private readonly GameClientConnection _connection;
    public List<GsiGameHandle> GameHandles { get; }

    private bool _stateSent;

    public GsiIcueHandler(GameClientConnection connection)
    {
        _connection = connection;
        GameHandles =
        [
            new GsiGameHandle(
                isMatch: message => message.StartsWith("{\"result\":true}"),
                doHandle: SetGame
            ),

            new GsiGameHandle(
                isMatch: message => message.Contains("\"breakingChanges\":false"),
                doHandle: RequestControl
            ),

            new GsiGameHandle(
                isMatch: message => message.Contains("\"serverProtocolVersion\":1"),
                doHandle: SetState
            )
        ];
        
        connection.MessageReceived += GameOutReceived;
        connection.CallbackReceived += GameCallbackReceived;
    }
    
    public async Task Start()
    {
        Console.WriteLine("[GsiGame] Sending handshake message to gameIn pipe...");
        const string handshakeMessage = """{"method":"CgSdkPerfromProtocolHandshake","params":{"gameSdkProtocolVersion":1}}""";
        await _connection.SendMessage(handshakeMessage);
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

    private async Task SetGame(string message)
    {
        await _connection.SendMessage("""{"method":"CgSdkSetGame","params":{"name":"Ghostrunner"}}""");
    }

    private async Task RequestControl(string message)
    {
        await _connection.SendMessage("""{"method":"CgSdkRequestControl"}""");
    }

    private async Task SetState(string message)
    {
        if (_stateSent)
        {
            return;
        }

        _stateSent = true;
        await _connection.SendMessage("""{"method":"CgSdkSetState","params":{"name":"SDKL_ScreenReactive"}}""");
    }
}