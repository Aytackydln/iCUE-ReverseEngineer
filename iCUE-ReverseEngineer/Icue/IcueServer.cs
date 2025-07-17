using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace iCUE_ReverseEngineer.Icue;

public sealed class IcueServer: IDisposable, IAsyncDisposable
{
    // I don't know how this is generated... Works on my machine, but might not work on others.
    private const string Guid = "{4A48E328-2134-4D19-B31F-764ADEFB844C}";
    
    private readonly NamedPipeServerStream _utilityCallback = IpcLogger.CreateOutPipe($"CorsairUtilityEngine\\{Guid}_callback");
    private readonly NamedPipeServerStream _utilityInPipe;
    private readonly NamedPipeServerStream _utilityOut = IpcLogger.CreateOutPipe($"CorsairUtilityEngine\\{Guid}_out");
    
    private readonly List<GameHandler> _games = [];

    public IcueServer()
    {
        Console.WriteLine($"Creating pipe: CorsairUtilityEngine\\{Guid}_in");
        var ps = new PipeSecurity();
        ps.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User!, PipeAccessRights.FullControl, AccessControlType.Allow));
        _utilityInPipe = NamedPipeServerStreamAcl.Create(
            $"CorsairUtilityEngine\\{Guid}_in",
            PipeDirection.In,                     // PIPE_ACCESS_INBOUND
            maxNumberOfServerInstances: 1,        // nMaxInstances = 1
            transmissionMode: PipeTransmissionMode.Byte,
            options: PipeOptions.Asynchronous,    // FILE_FLAG_OVERLAPPED
            inBufferSize: 4096,
            outBufferSize: 4096,
            pipeSecurity: ps
        );
    }

    public async Task Run()
    {
        _utilityCallback.BeginWaitForConnection(UtilityCallback, _utilityCallback);
        _utilityInPipe.BeginWaitForConnection(UtilityIn, _utilityInPipe);
        _utilityOut.BeginWaitForConnection(UtilityOut, _utilityOut);
    }

    private static void UtilityCallback(IAsyncResult ar)
    {
        Console.WriteLine("[UtilityCallback] connection");
    }

    private static void UtilityOut(IAsyncResult ar)
    {
        Console.WriteLine("[UtilityOut] connection");
    }

    async void UtilityIn(IAsyncResult ar)
    {
        Console.WriteLine("[UtilityIn] connection");
        var pipe = (NamedPipeServerStream)ar.AsyncState!;
        
        // read pipe message
        var buffer = new byte[4096];
        var bytesRead = await pipe.ReadAsync(buffer);
        if (bytesRead < 4)
        {
            Console.WriteLine($"[UtilityIn] Received message is too short to decode. ({bytesRead})");
            _ = Task.Run(() =>
            {
                pipe.Disconnect();
                pipe.BeginWaitForConnection(UtilityIn, pipe);
            });
            return;
        }

        var msgLen = BitConverter.ToInt32(buffer, 0);
        var gamePid = Encoding.UTF8.GetString(buffer, 4, msgLen).Trim('\0');
        Console.WriteLine($"[UtilityIn] Decoded message (len {msgLen}): {gamePid}");

        var gameConnection = new IcueToGameConnection(gamePid);
        var gameHandler = new GameHandler(gameConnection);
        gameHandler.GameDisconnected += HandleGameDisconnected;
        _games.Add(gameHandler);
        gameConnection.Run();

        await Task.Delay(1200);
        var pipeNameMessage = $@"\\.\pipe\{gamePid}\{{4e0c98fd-e062-49d6-8f02-277ac54ead5d}}";
        SendUtilityOut(pipeNameMessage);

        //run in another thread to reset stack
        _ = Task.Run(() =>
        {
            pipe.BeginWaitForConnection(UtilityIn, pipe);
        });
    }

    private void RestartGameWait()
    {
        Console.WriteLine("Restarting wait for connection.");
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
        _games.Remove((GameHandler)sender);
        RestartGameWait();
    }

    void SendUtilityOut(string message)
    {
        Console.WriteLine($"[UtilityOut] Sending:\n{message}");
        var msgBytes = Encoding.UTF8.GetBytes(message + "\0"); // null-terminated
        // 4-byte length prefix (little endian)
        var lengthPrefix = BitConverter.GetBytes(msgBytes.Length);
        _utilityOut.Write(lengthPrefix, 0, lengthPrefix.Length);
        _utilityOut.Write(msgBytes, 0, msgBytes.Length);
        _utilityOut.Flush();
        Console.WriteLine("Utility Out message sent.");
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