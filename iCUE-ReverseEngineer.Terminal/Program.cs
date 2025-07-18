using iCUE_ReverseEngineer_Terminal;
using iCUE_ReverseEngineer.Client;

const string clientArg = "--client";
const string sdkArg = "--sdk";
const string serverArg = "--server";
const string keyIdTestArg = "--keyIdTest";

var arg1 = args.Length > 0 ? args[0] : null;

switch (arg1)
{
    case null:
        // If no argument is provided, print usage and exit
        Console.WriteLine("Usage: iCUE-ReverseEngineer [--client | --server]");
        Console.WriteLine("Running as server by default.");
        StartServer();
        break;
    case serverArg:
    {
        // Start the server
        StartServer();
        break;
    }
    case clientArg:
    {
        // Start the client
        Console.WriteLine("Starting as GSI client...");
        var gameClient = new GsiClient(ClientType.Gsi);
        await gameClient.Run();
        break;
    }
    case sdkArg:
    {
        // Start the SDK client
        Console.WriteLine("Starting as SDK client...");
        var gameClient = new GsiClient(ClientType.Sdk);
        await gameClient.Run();
        break;
    }
    case keyIdTestArg:
    {
        // Run the KeyIdTest
        Console.WriteLine("Running KeyIdTest...");
        var keyIdTest = new KeyIdTest();
        await keyIdTest.Run();
        break;
    }
    default:
        Console.WriteLine($"Unknown argument: {arg1}");
        break;
}

Console.ReadLine();

void StartServer()
{
    Console.WriteLine("Starting as server...");
    var icueServer = new IcueTerminalServer();
    icueServer.Run();
}