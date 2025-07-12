using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json.Serialization;

namespace iCUE_ReverseEngineer.Icue;

public class IcueServer
{
    public EventHandler<string>? UtilityOutMessage;
    
    private readonly NamedPipeServerStream _utilityCallback = IpcLogger.CreateOutPipe("CorsairUtilityEngine\\{4A48E328-2134-4D19-B31F-764ADEFB844C}_callback");
    private readonly NamedPipeServerStream _utilityInPipe;
    private readonly NamedPipeServerStream _utilityOut = IpcLogger.CreateOutPipe("CorsairUtilityEngine\\{4A48E328-2134-4D19-B31F-764ADEFB844C}_out");
    
    private readonly List<IcueToGameConnection> _gameConnections = new();

    public IcueServer()
    {
        Console.WriteLine("Creating pipe: " + "CorsairUtilityEngine\\{4A48E328-2134-4D19-B31F-764ADEFB844C}_in");
        var ps = new PipeSecurity();
        ps.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User!, PipeAccessRights.FullControl, AccessControlType.Allow));
        _utilityInPipe = NamedPipeServerStreamAcl.Create(
            "CorsairUtilityEngine\\{4A48E328-2134-4D19-B31F-764ADEFB844C}_in",
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

    private void UtilityCallback(IAsyncResult ar)
    {
        Console.WriteLine("Utility Callback connection");
        
        // apparently this pipe is never used

        Console.WriteLine("UtilityCallback Exited");
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
        _gameConnections.Add(gameConnection);
        await gameConnection.Run();

        await Task.Delay(1200);
        var pipeNameMessage = $@"\\.\pipe\{gamePid}\{{4e0c98fd-e062-49d6-8f02-277ac54ead5d}}";
        UtilityOutMessage?.Invoke(this, pipeNameMessage);

        //run in another thread to reset stack
        Task.Run(() =>
        {
            pipe.BeginWaitForConnection(UtilityIn, pipe);
        });
    }

    void UtilityOut(IAsyncResult ar)
    {
        Console.WriteLine("[UtilityOut] connection");
        var pipe = (NamedPipeServerStream)ar.AsyncState!;
        
        UtilityOutMessage += (sender, message) =>
        {
            Console.WriteLine($"[UtilityOut] Sending:\n{message}");
            var msgBytes = Encoding.UTF8.GetBytes(message + "\0"); // null-terminated
            // 4-byte length prefix (little endian)
            var lengthPrefix = BitConverter.GetBytes(msgBytes.Length);
            pipe.Write(lengthPrefix, 0, lengthPrefix.Length);
            pipe.Write(msgBytes, 0, msgBytes.Length);
            pipe.Flush();
            Console.WriteLine("Utility Out message sent.");
        };

        Console.WriteLine("[UtilityOut] Exited");
    }

}