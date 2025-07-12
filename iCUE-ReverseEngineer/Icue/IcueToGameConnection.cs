using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace iCUE_ReverseEngineer.Icue;

public class IcueToGameConnection(string gamePid)
{
    public EventHandler<byte[]>? GameOutMessage;

    // GUIDs are randomly generated for each game instance, but we use a fixed one here for simplicity.
    private readonly NamedPipeServerStream _inPipe = IpcLogger.CreateInPipe($"{gamePid}\\{{4e0c98fd-e062-49d6-8f02-277ac54ead5d}}_in");
    private readonly NamedPipeServerStream _outPipe = IpcLogger.CreateOutPipe($"{gamePid}\\{{4e0c98fd-e062-49d6-8f02-277ac54ead5d}}_out");
    private readonly NamedPipeServerStream _callbackPipe = IpcLogger.CreateOutPipe($"{gamePid}\\{{4e0c98fd-e062-49d6-8f02-277ac54ead5d}}_callback");

    public async Task Run()
    {
        _callbackPipe.BeginWaitForConnection(GameCallback, _callbackPipe);
        _inPipe.BeginWaitForConnection(GameIn, _inPipe);
        _outPipe.BeginWaitForConnection(GameOut, _outPipe);
    }

    private void GameCallback(IAsyncResult ar)
    {
        Console.ForegroundColor = ConsoleColor.Black;
        Console.BackgroundColor = ConsoleColor.White;
        Console.WriteLine("GameCallback connection");
        Console.ResetColor();

        var pipe = (NamedPipeServerStream)ar.AsyncState!;

        Console.BackgroundColor = ConsoleColor.Red;
        Console.WriteLine("GameCallback Exited");
        Console.ResetColor();
        Task.Run(() =>
        {
            pipe.Disconnect();
            pipe.BeginWaitForConnection(GameCallback, pipe);
        });
    }

    private void GameIn(IAsyncResult ar)
    {
        Console.ForegroundColor = ConsoleColor.Black;
        Console.BackgroundColor = ConsoleColor.White;
        Console.WriteLine("GameIn connection");
        Console.ResetColor();
        var pipe = (NamedPipeServerStream)ar.AsyncState!;

        var buffer = new byte[4096];

        while (pipe.Read(buffer, 0, 4) > 0)
        {
            var length = BitConverter.ToInt32(buffer, 0);
            if (length == 0)
            {
                Console.WriteLine("[GameIn] Received empty message.");
                continue; // Skip empty messages
            }

            if (length < 4 || length > buffer.Length)
            {
                Console.WriteLine($"[GameIn] Invalid message length: {length}");
                continue; // Skip invalid messages
            }

            // Read the rest of the message
            var read = pipe.Read(buffer, 0, length);
            if (read < length)
            {
                Console.WriteLine($"[GameIn] Incomplete message received. Expected {length}, got {read}.");
                continue; // Skip incomplete messages
            }

            Array.Copy(buffer, 0, buffer, 0, length);

            // Decode and clean message
            var jsonRaw = Encoding.UTF8.GetString(buffer, 0, length).Trim('\0');

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[GameIn] {length}:\n{jsonRaw}");
            Console.ResetColor();

            try
            {
                var message = JsonSerializer.Deserialize(jsonRaw, IcueJsonContext.Default.IcueGameMessage);
                var messageMethod = message?.Method;
                switch (messageMethod)
                {
                    case "CgSdkPerfromProtocolHandshake":
                    {
                        var handshakeOkay = """{"serverProtocolVersion":1,"serverVersion":"5.30.90","breakingChanges":false}""";
                        var handshakeBytes = Encoding.UTF8.GetBytes(handshakeOkay + "\0"); // null-terminated
                        GameOutMessage?.Invoke(this, handshakeBytes); // handshake ack
                        break;
                    }
                    case "CgSdkRequestControl":
                    {
                        // send {"result":true}
                        var controlGranted = """{"result":true}""";
                        var controlBytes = Encoding.UTF8.GetBytes(controlGranted + "\0"); // null-terminated
                        GameOutMessage?.Invoke(this, controlBytes); // control granted
                        break;
                    }
                    case "CgSdkSetGame":
                    case "CgSdkSetState":
                    case "CgSdkSetEvent":
                    case "CgSdkClearAllEvents":
                    case "CgSdkClearAllStates":
                    case "CgSdkReleaseControl":
                    {
                        RespondOk();
                        break;
                    }
                    default:
                        Console.WriteLine($"[GameIn] unhandled message: {message.Method}");
                        break;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON parse failed: {ex.Message}");

                // send {"result":true,"errorCode":0} TODO temp
                var setStateResponse = """{"result":true,"errorCode":0}""";
                var setStateBytes = Encoding.UTF8.GetBytes(setStateResponse + "\0");
                GameOutMessage?.Invoke(this, setStateBytes);
            }

            //clear the buffer for the next read
            pipe.Flush();
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("GameIn Exited");
        Console.ResetColor();
        Task.Run(() =>
        {
            pipe.Disconnect();
            pipe.BeginWaitForConnection(GameIn, pipe);
        });
    }

    private void RespondOk()
    {
        // send {"result":true,"errorCode":0}
        var setGameResponse = """{"result":true,"errorCode":0}""";
        var setGameBytes = Encoding.UTF8.GetBytes(setGameResponse + "\0");
        GameOutMessage?.Invoke(this, setGameBytes); // game set response
    }

    private void GameOut(IAsyncResult ar)
    {
        Console.WriteLine("GameOut connection");
        var pipe = (NamedPipeServerStream)ar.AsyncState!;

        GameOutMessage += (sender, message) =>
        {
            var messageStr = Encoding.UTF8.GetString(message);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Sending Game Out:\n{messageStr}");
            Console.ResetColor();
            var messageLength = BitConverter.GetBytes(message.Length);

            var fullMessage = new byte[messageLength.Length + message.Length];
            Array.Copy(messageLength, 0, fullMessage, 0, messageLength.Length);
            Array.Copy(message, 0, fullMessage, messageLength.Length, message.Length);

            pipe.Write(fullMessage);
            pipe.Flush();
            Console.WriteLine("Game Out message sent.");
        };

        Console.WriteLine("GameOut Exited");
    }
}