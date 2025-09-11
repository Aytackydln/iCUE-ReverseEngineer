using System.IO.Pipes;
using System.Text;

namespace iCUE_ReverseEngineer.Icue;

public sealed class IcueServer : IDisposable, IAsyncDisposable
{
    // I don't know how this is generated... Works on my machine, but might not work on others.
    private const string Guid = "{4A48E328-2134-4D19-B31F-764ADEFB844C}";

    /// <summary>
    /// For observing. Called when a game connects to the callback pipe.
    /// </summary>
    public event EventHandler? CallbackPipeConnected;

    /// <summary>
    /// For observing. Called when a game connects to the output pipe.
    /// </summary>
    public event EventHandler? OutputConnected;

    public event EventHandler<GameHandler>? GameConnected;
    public event EventHandler<GameHandler>? GameDisconnected;

    private readonly NamedPipeServerStream _utilityCallback = IpcFactory.CreateOutPipe($"CorsairUtilityEngine\\{Guid}_callback");
    private readonly NamedPipeServerStream _utilityInPipe = IpcFactory.CreateInPipe($"CorsairUtilityEngine\\{Guid}_in");
    private readonly NamedPipeServerStream _utilityOut = IpcFactory.CreateOutPipe($"CorsairUtilityEngine\\{Guid}_out");

    private readonly List<GameHandler> _games = [];

    public void Run()
    {
        _utilityCallback.BeginWaitForConnection(UtilityCallback, _utilityCallback);
        _utilityInPipe.BeginWaitForConnection(UtilityIn, _utilityInPipe);
        _utilityOut.BeginWaitForConnection(UtilityOut, _utilityOut);
    }

    private void UtilityCallback(IAsyncResult ar)
    {
        CallbackPipeConnected?.Invoke(this, EventArgs.Empty);
    }

    private void UtilityOut(IAsyncResult ar)
    {
        OutputConnected?.Invoke(this, EventArgs.Empty);
    }

    private void UtilityIn(IAsyncResult ar)
    {
        var pipe = (NamedPipeServerStream)ar.AsyncState!;
        if (!pipe.IsConnected)
        {
            return;
        }

        var buffer = new byte[4096];
        var bytesRead = pipe.Read(buffer);
        if (bytesRead < 4)
        {
            _ = Task.Run(() =>
            {
                pipe.Disconnect();
            });
            return;
        }

        var msgLen = BitConverter.ToInt32(buffer, 0);
        // gamePid like :pid:71908
        var gamePidString = Encoding.UTF8.GetString(buffer, 4, msgLen).Trim('\0');

        var gameConnection = new IcueToGameConnection(gamePidString);
        var gameHandler = new GameHandler(gameConnection);
        gameHandler.GameDisconnected += HandleGameDisconnected;
        _games.Add(gameHandler);
        gameConnection.Run();
        GameConnected?.Invoke(this, gameHandler);

        var pipeNameMessage = $@"\\.\pipe\{gamePidString}\{{4e0c98fd-e062-49d6-8f02-277ac54ead5d}}";
        SendUtilityOut(pipeNameMessage);

        // Restart the wait for connection on the input pipe
        _ = Task.Run(() => { pipe.BeginWaitForConnection(UtilityIn, pipe); });
    }

    private void RestartGameWait()
    {
        if (_utilityOut.IsConnected)
        {
            _utilityOut.Write([]);
            _utilityOut.Flush();
            _utilityOut.Disconnect();
        }

        _utilityOut.BeginWaitForConnection(UtilityOut, _utilityOut);
        _utilityCallback.BeginWaitForConnection(UtilityCallback, _utilityCallback);
    }

    private void HandleGameDisconnected(object? sender, EventArgs e)
    {
        var gameHandler = (GameHandler)sender!;
        gameHandler.GameDisconnected -= HandleGameDisconnected;
        _games.Remove(gameHandler);
        GameDisconnected?.Invoke(this, gameHandler);
        RestartGameWait();
    }

    void SendUtilityOut(string message)
    {
        var msgBytes = Encoding.UTF8.GetBytes(message + "\0"); // null-terminated
        var lengthPrefix = BitConverter.GetBytes(msgBytes.Length);
        _utilityOut.Write(lengthPrefix, 0, lengthPrefix.Length);
        _utilityOut.Write(msgBytes, 0, msgBytes.Length);
        _utilityOut.Flush();
    }

    public void Dispose()
    {
        _utilityCallback.Dispose();
        _utilityInPipe.Dispose();
        _utilityOut.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var gameHandler in _games)
        {
            gameHandler.Dispose();
        }

        await _utilityCallback.DisposeAsync();
        await _utilityInPipe.DisposeAsync();
        await _utilityOut.DisposeAsync();
    }
}