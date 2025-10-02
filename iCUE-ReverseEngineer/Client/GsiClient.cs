using System.Text.RegularExpressions;
using iCUE_ReverseEngineer.Client.Gsi;
using iCUE_ReverseEngineer.Client.Sdk;

namespace iCUE_ReverseEngineer.Client;

public enum ClientType
{
    Gsi,
    Sdk,
    OldSdk,
}

public sealed partial class GsiClient(ClientType clientType)
{
    private ClientToIcueConnection? _icueConnection;
    private GameClientConnection? _gameClientConnection;
    private IIcueHandler? _handler;

    public async Task Run()
    {
        var pipeDir = new DirectoryInfo(@"\\.\pipe\");
        var allPipes = pipeDir.GetFileSystemInfos();

        var corsairOutPipeName = allPipes.FirstOrDefault(n => CorsairOutPipe().IsMatch(n.Name));
        var corsairCallbackPipeName = allPipes.FirstOrDefault(n => CorsairCallbackPipe().IsMatch(n.Name));
        var corsairInPipeName = allPipes.FirstOrDefault(n => CorsairInPipe().IsMatch(n.Name));

        if (corsairOutPipeName == null || corsairCallbackPipeName == null || corsairInPipeName == null)
        {
            throw new InvalidOperationException("Could not find all required iCUE pipes.");
        }

        _icueConnection = new ClientToIcueConnection(
            corsairOutPipeName.Name,
            corsairCallbackPipeName.Name,
            corsairInPipeName.Name
        );
        _icueConnection.GameConnected += async (_, args) =>
        {
            _gameClientConnection = new GameClientConnection(args.PipePrefix);
            _handler = clientType switch
            {
                ClientType.Gsi => new GsiIcueHandler(_gameClientConnection),
                ClientType.Sdk => new SdkIcueHandler(_gameClientConnection),
                ClientType.OldSdk => new OldSdkIcueHandler(_gameClientConnection),
                _ => throw new ArgumentOutOfRangeException(nameof(clientType), clientType, null)
            };
            await _gameClientConnection.Start();
            await _handler.Start();
        };
        await _icueConnection.Start();
    }

    public async Task SetColor(int ledIndex, int red, int green, int blue)
    {
        if (_gameClientConnection == null)
        {
            throw new InvalidOperationException("iCUE connection is not established.");
        }

        var previousLed = ledIndex - 1;
        var ledColor = $$$"""{"id":{{{ledIndex}}},"method":"CorsairSetLedsColors","params":{"ledsColors":"{{{ledIndex}}},{{{red}}},{{{green}}},{{{blue}}}"}}""";
        await _gameClientConnection.SendMessage(ledColor);
    }

    [GeneratedRegex(@"^CorsairUtilityEngine\\.*_in$", RegexOptions.IgnoreCase, "en-GB")]
    private static partial Regex CorsairInPipe();

    [GeneratedRegex(@"^CorsairUtilityEngine\\.*_out$", RegexOptions.IgnoreCase, "en-GB")]
    private static partial Regex CorsairOutPipe();

    [GeneratedRegex(@"^CorsairUtilityEngine\\.*_callback$", RegexOptions.IgnoreCase, "en-GB")]
    private static partial Regex CorsairCallbackPipe();
}