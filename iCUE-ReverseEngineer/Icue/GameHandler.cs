namespace iCUE_ReverseEngineer.Icue;

public sealed class GameHandler: IDisposable
{
    public event EventHandler? GameDisconnected;
    
    private readonly IcueToGameConnection _gameConnection;

    private readonly HashSet<string> _states = [];
    private readonly HashSet<string> _events = [];

    public GameHandler(IcueToGameConnection gameConnection)
    {
        _gameConnection = gameConnection;

        _gameConnection.GameMessageReceived += OnGameMessageReceived;
        _gameConnection.GameDisconnected += OnGameDisconnected;
    }

    private void OnGameDisconnected(object? sender, EventArgs e)
    {
        // print states and events
        Console.WriteLine("Received States:");
        foreach (var state in _states)
        {
            Console.WriteLine($"- {state}");
        }
        Console.WriteLine("Received Events:");
        foreach (var gameEvent in _events)
        {
            Console.WriteLine($"- {gameEvent}");
        }

        _gameConnection.Close();
        GameDisconnected?.Invoke(this, EventArgs.Empty);
    }

    private void OnGameMessageReceived(object? sender, IcueGameMessage message)
    {
        var messageMethod = message.Method;
        switch (messageMethod)
        {
            case "CgSdkPerfromProtocolHandshake":
            {
                const string handshakeOkay = """{"serverProtocolVersion":1,"serverVersion":"5.30.90","breakingChanges":false}""";
                _gameConnection.SendGameMessage(handshakeOkay);
                break;
            }
            case "CgSdkRequestControl":
            {
                const string controlGranted = """{"result":true}""";
                _gameConnection.SendGameMessage(controlGranted);
                break;
            }
            case "CgSdkSetState":
            {
                var stateName = message.Params?.Name;
                if (stateName != null)
                {
                    _states.Add(stateName);
                }

                RespondOk();
                break;
            }
            case "CgSdkSetEvent":
            {
                var eventName = message.Params?.Name;
                if (eventName != null)
                {
                    _events.Add(eventName);
                }

                RespondOk();
                break;
            }
            case "CgSdkSetGame":
            case "CgSdkClearState":
            case "CgSdkClearAllEvents":
            case "CgSdkClearAllStates":
            case "CgSdkReleaseControl":
            {
                RespondOk();
                break;
            }
            default:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[GameIn] unhandled message: {message.Method}");
                Console.ResetColor();
                break;
        }
    }

    private void RespondOk()
    {
        const string setGameResponse = """{"result":true,"errorCode":0}""";
        _gameConnection.SendGameMessage(setGameResponse);
    }

    public void Dispose()
    {
        _gameConnection.Dispose();
    }
}