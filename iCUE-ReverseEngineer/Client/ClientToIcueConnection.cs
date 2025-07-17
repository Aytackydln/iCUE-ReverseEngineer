using System.IO.Pipes;
using System.Text;

namespace iCUE_ReverseEngineer.Client;

public class ClientConnectedEventArgs(string pipePrefix) : EventArgs
{
    public string PipePrefix { get; } = pipePrefix;
}

public class ClientToIcueConnection(
    string corsairOutPipeName,
    string corsairCallbackPipeName,
    string corsairInPipeName
)
{
    public event EventHandler<ClientConnectedEventArgs>? GameConnected;
    
    private readonly NamedPipeClientStream _corsairOutListener = new(".", corsairOutPipeName, PipeDirection.In, PipeOptions.Asynchronous);
    private readonly NamedPipeClientStream _corsairCallbackListener = new(".", corsairCallbackPipeName, PipeDirection.In, PipeOptions.Asynchronous);
    private readonly NamedPipeClientStream _corsairInPipe = new(".", corsairInPipeName, PipeDirection.Out, PipeOptions.Asynchronous);

    private readonly byte[] _corsairOutBuffer = new byte[4096];
    private readonly byte[] _corsairCallbackBuffer = new byte[4096];
    
    public async Task Start()
    {
        // wait for Game Pipe prefix from Corsair Utility Engine
        await _corsairOutListener.ConnectAsync();
        Console.WriteLine("Corsair Out Pipe connected.");
        CorsairOutTask();

        await _corsairCallbackListener.ConnectAsync();
        Console.WriteLine("Corsair Callback Pipe connected.");
        CorsairCallbackTask();

        Console.WriteLine("Delaying...\n\n");
        await Task.Delay(1000); // Wait for the listeners to start

        await _corsairInPipe.ConnectAsync();
        Console.WriteLine("Corsair In Pipe connected.");

        await SendPid();
    }

    private async Task SendPid()
    {
        var pid = Environment.ProcessId;
        await SendInMessage($":pid:{pid}");
    }

    private async Task SendInMessage(string message)
    {
        Console.WriteLine($"[CorsairIn]:\n{message}");
        if (!_corsairInPipe.IsConnected)
        {
            Console.WriteLine("[CorsairIn] Corsair In Pipe is not connected.");
            return;
        }

        var msgBytes = Encoding.UTF8.GetBytes(message + "\0"); // null-terminated
        // 4-byte length prefix (little endian)
        var lengthPrefix = BitConverter.GetBytes(msgBytes.Length);
        
        await _corsairInPipe.WriteAsync(lengthPrefix);
        await _corsairInPipe.WriteAsync(msgBytes);
        await _corsairInPipe.FlushAsync();
    }

    private void CorsairOutTask()
    {
        _ = Task.Run(async () =>
        {
            var lengthRead = await _corsairOutListener.ReadAsync(_corsairOutBuffer);
            if (lengthRead == 0)
            {
                Console.WriteLine("[CorsairOut] Received empty message.");
                CorsairOutTask();
                return; // Skip empty messages
            }
            var length = BitConverter.ToInt32(_corsairOutBuffer, 0);
            
            var messageRead = await _corsairOutListener.ReadAsync(_corsairOutBuffer);
            var fullPipePrefix = Encoding.UTF8.GetString(_corsairOutBuffer[..length]).Trim('\0');

            // Extract the prefix from the full pipe name
            var pipePrefix = fullPipePrefix[9..];
            Console.WriteLine($"[CorsairOut] pipePrefix ({length}):\n" + pipePrefix);

            // ack by writing to the Corsair in pipe?
            _corsairInPipe.Write([], 0, 0);
            
            GameConnected?.Invoke(this, new ClientConnectedEventArgs(pipePrefix));
        });
    }

    private void CorsairCallbackTask()
    {
        _ = Task.Run(async () =>
        {
            var lengthRead = await _corsairCallbackListener.ReadAsync(_corsairCallbackBuffer);

            var length = BitConverter.ToInt32(_corsairCallbackBuffer, 0);
            if (length <= 0 || length > _corsairCallbackBuffer.Length)
            {
                await Console.Error.WriteLineAsync($"[GameCallback] Invalid message length: {length}");
                return;
            }

            var messageRead = await _corsairCallbackListener.ReadAsync(_corsairCallbackBuffer);
            var json = Encoding.UTF8.GetString(_corsairCallbackBuffer, 4, length).Trim('\0');
            Console.WriteLine($"[CorsairCallback]:\n{json}");

            CorsairCallbackTask();
        });
    }
}