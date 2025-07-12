using System.IO.Pipes;
using System.Text;

namespace iCUE_ReverseEngineer.Game;

public class GameClientConnection(string pipePrefix)
{
    private bool _stateSent;

    private EventHandler<string>? _gameInMessageEvent;

    private readonly IcuePipeReader _gameOutPipe = new($"{pipePrefix}_out");
    private readonly IcuePipeReader _gameCallbackPipe = new($"{pipePrefix}_callback");

    public async Task Start()
    {
        var gameInPipe = new NamedPipeClientStream(".", $"{pipePrefix}_in", PipeDirection.Out, PipeOptions.Asynchronous);
        await gameInPipe.ConnectAsync();
        Console.WriteLine("Game In Pipe connected.");
        _gameInMessageEvent += (_, message) =>
        {
            Console.WriteLine($"Sending GameIn:\n{message}");
            var msgBytes = Encoding.UTF8.GetBytes(message + "\0"); // null-terminated
            // 4-byte length prefix (little endian)
            var lengthPrefix = BitConverter.GetBytes(msgBytes.Length);

            gameInPipe.Write(lengthPrefix);
            gameInPipe.Write(msgBytes);
            gameInPipe.Flush();
            Console.WriteLine("Game In message sent.");
        };

        _gameOutPipe.MessageReceived += GameOutReceived;
        await _gameOutPipe.Start();

        _gameCallbackPipe.MessageReceived += GameCallbackReceived;
        await _gameCallbackPipe.Start();

        Console.WriteLine("Sending handshake message to gameIn pipe...");
        var json = """{"method":"CgSdkPerfromProtocolHandshake","params":{"gameSdkProtocolVersion":1}} """;
        var handshakeBytes = Encoding.UTF8.GetBytes(json); // null-terminated
        var handshakeLengthPrefix = BitConverter.GetBytes(handshakeBytes.Length);
        Console.WriteLine("Game In Pipe connected.");
        await gameInPipe.WriteAsync(handshakeLengthPrefix);
        await gameInPipe.WriteAsync(handshakeBytes);
        await gameInPipe.FlushAsync();
        gameInPipe.WaitForPipeDrain();
        Console.WriteLine("Sent handshake message to gameIn pipe.");
    }

    private void GameOutReceived(object? sender, string message)
    {
        Console.WriteLine($"[GameOut]:\n{message}");

        if (message.StartsWith("{\"result\":true}"))
        {
            _gameInMessageEvent?.Invoke(this, """{"method":"CgSdkSetGame","params":{"name":"Ghostrunner"}}""");
        }
        // expect {"serverProtocolVersion":1,"serverVersion":"5.30.90","breakingChanges":false}
        else if (message.Contains("\"breakingChanges\":false"))
        {
            _gameInMessageEvent?.Invoke(this, """{"method":"CgSdkRequestControl"}""");
        }
        else if (message.StartsWith("""{"result":true,"errorCode":0}"""))
        {
            if (!_stateSent)
            {
                _stateSent = true;
                _gameInMessageEvent?.Invoke(this, """{"method":"CgSdkSetState","params":{"name":"SDKL_ScreenReactive"}}""");
            }
        }
        else
        {
            Console.WriteLine($"[GameOut] unhandled message: {message}");
        }
    }

    private static void GameCallbackReceived(object? sender, string message)
    {
        Console.WriteLine($"[GameCallback]:\n{message}");
    }
}