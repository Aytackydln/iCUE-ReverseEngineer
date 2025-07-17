using iCUE_ReverseEngineer.Icue.Data;
using iCUE_ReverseEngineer.Icue.Gsi;
using iCUE_ReverseEngineer.Icue.Sdk;

namespace iCUE_ReverseEngineer.Icue;

public sealed class GameHandler : IDisposable
{
    public event EventHandler? GameDisconnected;

    private readonly IcueToGameConnection _gameConnection;

    private readonly GsiHandler _gsiHandler;
    private readonly Dictionary<string, Action<IcueGameMessage>> _handles;

    public GameHandler(IcueToGameConnection gameConnection)
    {
        _gameConnection = gameConnection;
        _gsiHandler = new GsiHandler(_gameConnection);
        var sdkHandler = new SdkHandler(_gameConnection);
        _handles = _gsiHandler.GameHandles.Concat(sdkHandler.SdkHandles)
            .ToDictionary(x => x.Key, x => x.Value);

        _gameConnection.GameMessageReceived += OnGameMessageReceived;
        _gameConnection.GameDisconnected += OnGameDisconnected;
    }

    private void OnGameDisconnected(object? sender, EventArgs e)
    {
        // print states and events
        Console.WriteLine("Received States:");
        foreach (var state in _gsiHandler.States)
        {
            Console.WriteLine($"- {state}");
        }

        Console.WriteLine("Received Events:");
        foreach (var gameEvent in _gsiHandler.Events)
        {
            Console.WriteLine($"- {gameEvent}");
        }

        _gameConnection.Close();
        GameDisconnected?.Invoke(this, EventArgs.Empty);
    }

    private void OnGameMessageReceived(object? sender, IcueGameMessage message)
    {
        var messageMethod = message.Method;
        if (!_handles.TryGetValue(messageMethod, out var handle))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[GameIn] unhandled message: {message.Method}");
            Console.ResetColor();
            return;
        }

        handle(message);
    }

    public void Dispose()
    {
        _gameConnection.Dispose();
    }
}