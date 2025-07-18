using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using iCUE_ReverseEngineer.Icue.Data;

namespace iCUE_ReverseEngineer.Icue;

public sealed class IcueToGameConnection(string gamePid) : IDisposable, IAsyncDisposable
{
    public event EventHandler<string>? GamePipeConnected;
    public event EventHandler<IcueGameMessage>? GameMessageReceived;
    public event EventHandler? GameDisconnected;
    
    public string GamePid { get; } = gamePid;

    // GUIDs are randomly generated for each game instance, but we use a fixed one here for simplicity.
    private readonly NamedPipeServerStream _inPipe = IpcFactory.CreateInPipe($"{gamePid}\\{{4e0c98fd-e062-49d6-8f02-277ac54ead5d}}_in");
    private readonly NamedPipeServerStream _outPipe = IpcFactory.CreateOutPipe($"{gamePid}\\{{4e0c98fd-e062-49d6-8f02-277ac54ead5d}}_out");
    private readonly NamedPipeServerStream _callbackPipe = IpcFactory.CreateOutPipe($"{gamePid}\\{{4e0c98fd-e062-49d6-8f02-277ac54ead5d}}_callback");

    public void Run()
    {
        _callbackPipe.BeginWaitForConnection(LogConnection("GameCallback"), _callbackPipe);
        _inPipe.BeginWaitForConnection(GameIn, _inPipe);
        _outPipe.BeginWaitForConnection(LogConnection("GameOut"), _outPipe);
    }

    private AsyncCallback LogConnection(string gameCallback)
    {
        return _ => { GamePipeConnected?.Invoke(this, gameCallback); };
    }

    public void SendGameMessage(string messageStr)
    {
        var message = Encoding.UTF8.GetBytes(messageStr + "\0");
        var messageLength = BitConverter.GetBytes(message.Length);
   
        _outPipe.Write(messageLength);
        _outPipe.Write(message, 0, message.Length);
        _outPipe.Flush();
    }

    private void GameIn(IAsyncResult ar)
    {
        var pipe = (NamedPipeServerStream)ar.AsyncState!;

        var buffer = new byte[pipe.InBufferSize];

        while (pipe.Read(buffer, 0, 4) > 0)
        {
            var length = BitConverter.ToInt32(buffer, 0);
            if (length == 0)
            {
                continue; // Skip empty messages
            }

            if (length < 4 || length > buffer.Length)
            {
                continue; // Skip invalid messages
            }

            // Read the rest of the message
            var read = pipe.Read(buffer, 0, length);
            if (read < length)
            {
                continue; // Skip incomplete messages
            }

            // Decode and clean message
            var jsonRaw = Encoding.UTF8.GetString(buffer, 0, length).Trim('\0');

            try
            {
                var message = JsonSerializer.Deserialize(jsonRaw, IcueJsonContext.Default.IcueGameMessage);
                if (message == null)
                {
                    continue; // Skip null messages
                }

                GameMessageReceived?.Invoke(this, message);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[iCUE Replica] JSON parse failed: {ex.Message}");
            }
        }

        // Notify that the game has disconnected
        GameDisconnected?.Invoke(this, EventArgs.Empty);
    }

    public void Close()
    {
        _inPipe.Close();
        _outPipe.Close();
        _callbackPipe.Close();
    }

    public void Dispose()
    {
        _inPipe.Dispose();
        _outPipe.Dispose();
        _callbackPipe.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _inPipe.DisposeAsync();
        await _outPipe.DisposeAsync();
        await _callbackPipe.DisposeAsync();
    }
}