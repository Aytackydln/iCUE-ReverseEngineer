using System.IO.Pipes;
using System.Text;

namespace iCUE_ReverseEngineer.Client;

public class GameClientConnection
{
    public event EventHandler<string>? MessageReceived;
    public event EventHandler<string>? CallbackReceived;
    
    private readonly string _pipePrefix;

    private readonly IcuePipeReader _gameOutPipe;
    private readonly IcuePipeReader _gameCallbackPipe;
    private NamedPipeClientStream? _gameInPipe;

    public GameClientConnection(string pipePrefix)
    {
        _pipePrefix = pipePrefix;
        
        _gameOutPipe = new IcuePipeReader($"{pipePrefix}_out");
        _gameCallbackPipe = new IcuePipeReader($"{pipePrefix}_callback");

        _gameOutPipe.MessageReceived += OnMessageReceived;
        _gameCallbackPipe.MessageReceived += OnCallbackReceived;
    }

    private void OnMessageReceived(object? sender, string e)
    {
        MessageReceived?.Invoke(this, e);
    }

    private void OnCallbackReceived(object? sender, string e)
    {
        CallbackReceived?.Invoke(this, e);
    }

    public async Task Start()
    {
        _gameInPipe = new NamedPipeClientStream(".", $"{_pipePrefix}_in", PipeDirection.Out, PipeOptions.Asynchronous);
        await _gameInPipe.ConnectAsync();
        Console.WriteLine("[ClientConnection] Game In Pipe connected.");

        await _gameOutPipe.Start();
        await _gameCallbackPipe.Start();
    }

    public async Task SendMessage(string message)
    {
        if (_gameInPipe == null)
        {
            Console.WriteLine("[ClientConnection] Game In Pipe is not connected.");
            return;
        }
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[GameIn]:\n{message}");
        Console.ResetColor();
        var msgBytes = Encoding.UTF8.GetBytes(message + "\0"); // null-terminated
        var lengthPrefix = BitConverter.GetBytes(msgBytes.Length);
        
        await _gameInPipe.WriteAsync(lengthPrefix);
        await _gameInPipe.WriteAsync(msgBytes);
        await _gameInPipe.FlushAsync();
    }
}