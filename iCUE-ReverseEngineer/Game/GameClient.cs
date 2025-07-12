using System.Text.RegularExpressions;

namespace iCUE_ReverseEngineer.Game;

public partial class GameClient
{
    private GameToIcueConnection? _icueConnection;
    private GameClientConnection? _gameClientConnection;

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

        _icueConnection = new GameToIcueConnection(
            corsairOutPipeName.Name,
            corsairCallbackPipeName.Name,
            corsairInPipeName.Name
        );
        _icueConnection.GameConnected += async (_, args) =>
        {
            _gameClientConnection = new GameClientConnection(args.PipePrefix);
            await _gameClientConnection.Start();
        };
        await _icueConnection.Start();
    }

    [GeneratedRegex(@"^CorsairUtilityEngine\\.*_in$", RegexOptions.IgnoreCase, "en-GB")]
    private static partial Regex CorsairInPipe();

    [GeneratedRegex(@"^CorsairUtilityEngine\\.*_out$", RegexOptions.IgnoreCase, "en-GB")]
    private static partial Regex CorsairOutPipe();

    [GeneratedRegex(@"^CorsairUtilityEngine\\.*_callback$", RegexOptions.IgnoreCase, "en-GB")]
    private static partial Regex CorsairCallbackPipe();
}