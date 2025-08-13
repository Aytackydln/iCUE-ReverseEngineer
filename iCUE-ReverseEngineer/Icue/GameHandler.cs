using iCUE_ReverseEngineer.Icue.Data;
using iCUE_ReverseEngineer.Icue.Gsi;
using iCUE_ReverseEngineer.Icue.Sdk;

namespace iCUE_ReverseEngineer.Icue;

public sealed class IcueGameConnectedEventArgs(long gamePid) : EventArgs
{
    public long GamePid { get; } = gamePid;
}

public sealed class GameHandler : IDisposable
{
    /// <summary>
    /// For observing. Called when a game connects to the callback pipe.
    /// </summary>
    public event EventHandler<IcueGameConnectedEventArgs>? GamePipeConnected;

    public event EventHandler? GameDisconnected;

    private readonly IcueToGameConnection _gameConnection;

    public long GamePid => _gameConnection.GamePid;
    public GsiHandler GsiHandler { get; }
    public SdkHandler SdkHandler { get; }
    private readonly Dictionary<string, Action<IcueGameMessage>> _handles;

    internal GameHandler(IcueToGameConnection gameConnection)
    {
        _gameConnection = gameConnection;
        GsiHandler = new GsiHandler(_gameConnection);
        SdkHandler = new SdkHandler(_gameConnection);
        _handles = GsiHandler.GameHandles.Concat(SdkHandler.SdkHandles)
            .ToDictionary(x => x.Key, x => x.Value);

        _gameConnection.GamePipeConnected += GameConnectionOnGamePipeConnected;
        _gameConnection.GameMessageReceived += OnGameMessageReceived;
        _gameConnection.GameDisconnected += OnGameDisconnected;
    }

    private void GameConnectionOnGamePipeConnected(object? sender, string e)
    {
        GamePipeConnected?.Invoke(this, new IcueGameConnectedEventArgs(_gameConnection.GamePid));
        _gameConnection.Run();
    }

    private void OnGameDisconnected(object? sender, EventArgs e)
    {
        _gameConnection.Close();
        GameDisconnected?.Invoke(this, EventArgs.Empty);
    }

    private void OnGameMessageReceived(object? sender, IcueGameMessage message)
    {
        var messageMethod = message.Method;
        if (!_handles.TryGetValue(messageMethod, out var handle))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[iCUE Replica][GameIn] unhandled message: {message.Method}");
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