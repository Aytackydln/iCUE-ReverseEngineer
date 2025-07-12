using System.IO.Pipes;
using System.Text;

namespace iCUE_ReverseEngineer.Game;

public sealed class IcuePipeReader(string pipeName) : IDisposable, IAsyncDisposable
{
    public event EventHandler<string>? MessageReceived;
    
    private readonly NamedPipeClientStream _pipeClient = new(".", pipeName, PipeDirection.In, PipeOptions.Asynchronous);
    private readonly byte[] _gameOutBuffer = new byte[4096];

    public async Task Start()
    {
        await _pipeClient.ConnectAsync();
        Console.WriteLine(pipeName + " connected.");

        ReadTask();
    }

    private void ReadTask()
    {
        _ = Task.Run(async () =>
        {
            var lengthRead = await _pipeClient.ReadAsync(_gameOutBuffer);

            var length = BitConverter.ToInt32(_gameOutBuffer, 0);
            if (length <= 0)
            {
                await Console.Error.WriteLineAsync($"[{pipeName}] Invalid message length: {length}");
                return;
            }

            var messageRead = await _pipeClient.ReadAsync(_gameOutBuffer);
            var json = Encoding.UTF8.GetString(_gameOutBuffer).Trim('\0');
            MessageReceived?.Invoke(this, json);

            ReadTask();
        });
    }

    public void Dispose()
    {
        _pipeClient.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _pipeClient.DisposeAsync();
    }
}