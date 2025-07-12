
using iCUE_ReverseEngineer.Game;
using iCUE_ReverseEngineer.Icue;

const string clientArg = "--client";
const string serverArg = "--server";

var arg1 = args.Length > 0 ? args[0] : null;

switch (arg1)
{
    case null:
        // If no argument is provided, print usage and exit
        Console.WriteLine("Usage: iCUE-ReverseEngineer [--client | --server]");
        Console.WriteLine("Running as server by default.");
        await StartServer();
        break;
    case serverArg:
    {
        // Start the server
        await StartServer();
        break;
    }
    case clientArg:
    {
        // Start the client
        Console.WriteLine("Starting as client...");
        var gameClient = new GameClient();
        await gameClient.Run();
        break;
    }
    default:
        Console.WriteLine($"Unknown argument: {arg1}");
        break;
}

Console.ReadLine();

async Task StartServer()
{
    Console.WriteLine("Starting as server...");
    var icueServer = new IcueServer();
    await icueServer.Run();
}