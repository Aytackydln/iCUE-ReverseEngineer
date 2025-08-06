using System.IO.Pipes;
using System.Text;

namespace iCUE_ReverseEngineer.Client;

internal sealed class IcuePipeReader(string pipeName) : IDisposable, IAsyncDisposable
{
    internal event EventHandler<string>? MessageReceived;
    
    private readonly NamedPipeClientStream _pipeClient = new(".", pipeName, PipeDirection.In, PipeOptions.Asynchronous);

    internal async Task Start()
    {
        await _pipeClient.ConnectAsync();
        Console.WriteLine(pipeName + " connected.");

        ReadTask();
    }

    private void ReadTask()
    {
        _ = Task.Run(async () =>
        {
            var lengthBuffer = new byte[4];
            await _pipeClient.ReadExactlyAsync(lengthBuffer);

            var length = BitConverter.ToInt32(lengthBuffer, 0);
            if (length <= 0)
            {
                await Console.Error.WriteLineAsync($"[{pipeName}] Invalid message length: {length}");
                return;
            }

            var gameOutBuffer = new byte[length];
            await _pipeClient.ReadExactlyAsync(gameOutBuffer);
            var json = Encoding.UTF8.GetString(gameOutBuffer).Trim('\0');
            MessageReceived?.Invoke(this, json);

            ReadTask();
        });
    }

    void IDisposable.Dispose()
    {
        _pipeClient.Dispose();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await _pipeClient.DisposeAsync();
    }
}