using System.IO.Pipes;
using System.Text;

namespace iCUE_ReverseEngineer.Game;

public class GameConnectedEventArgs(string pipePrefix) : EventArgs
{
    public string PipePrefix { get; } = pipePrefix;
}

public class GameToIcueConnection(
    string corsairOutPipeName,
    string corsairCallbackPipeName,
    string corsairInPipeName
)
{
    public event EventHandler<GameConnectedEventArgs>? GameConnected;
    
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
        var msg = $":pid:{pid}\0"; // null-terminated
        var msgBytes = Encoding.UTF8.GetBytes(msg);

        var lengthPrefix = BitConverter.GetBytes(msgBytes.Length);
        var fullMessage = new byte[lengthPrefix.Length + msgBytes.Length];
        Array.Copy(lengthPrefix, fullMessage, lengthPrefix.Length);
        Array.Copy(msgBytes, 0, fullMessage, lengthPrefix.Length, msgBytes.Length);

        await _corsairInPipe.WriteAsync(fullMessage);
        await _corsairInPipe.FlushAsync();
        
        Console.WriteLine($"[CorsairIn] Sent PID: {pid}");
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
            
            GameConnected?.Invoke(this, new GameConnectedEventArgs(pipePrefix));
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