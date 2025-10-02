using iCUE_ReverseEngineer.Icue;
using iCUE_ReverseEngineer.Icue.Gsi;

namespace iCUE_ReverseEngineer_Terminal;

public class IcueTerminalServer
{
    private readonly IcueServer _icueServer = new();

    private HashSet<string> States { get; } = [];
    private HashSet<string> Events { get; } = [];

    public IcueTerminalServer()
    {
        _icueServer.CallbackPipeConnected += IcueServerOnCallbackPipeConnected;
        _icueServer.OutputConnected += IcueServerOnOutputConnected;
        _icueServer.GameConnected += IcueServerOnGameConnected;
        _icueServer.GameDisconnected += IcueServerOnGameDisconnected;
    }

    public void Run()
    {
        _icueServer.Run();
    }

    private static void IcueServerOnCallbackPipeConnected(object? sender, EventArgs e)
    {
        Console.WriteLine("[UtilityCallback] connection");
    }

    private static void IcueServerOnOutputConnected(object? sender, EventArgs e)
    {
        Console.WriteLine("[UtilityOut] connection");
    }

    private void IcueServerOnGameConnected(object? sender, GameHandler handler)
    {
        Console.WriteLine("A game has connected.");
        handler.GamePipeConnected += HandlerOnGamePipeConnected;
        handler.GsiHandler.StateAdded += GsiHandlerOnStateAdded;
        handler.GsiHandler.EventAdded += GsiHandlerOnEventAdded;
        handler.SdkHandler.ColorsUpdated += SdkHandlerOnColorsUpdated;
        handler.SdkHandler.GameConnected += SdkHandlerOnGameConnected;
    }

    private static void HandlerOnGamePipeConnected(object? sender, string s)
    {
        Console.WriteLine(s + " connection");
    }

    private void GsiHandlerOnStateAdded(object? sender, IcueStateEventArgs icueStateEventArgs)
    {
        if(States.Add(icueStateEventArgs.StateName))
        {
            Console.WriteLine($"State added: {icueStateEventArgs.StateName}");
        }
    }

    private void GsiHandlerOnEventAdded(object? sender, IcueStateEventArgs icueStateEventArgs)
    {
        if(Events.Add(icueStateEventArgs.StateName))
        {
            Console.WriteLine($"Event added: {icueStateEventArgs.StateName}");
        }
    }

    private static void SdkHandlerOnColorsUpdated(object? sender, EventArgs e)
    {
        Console.WriteLine("Received colors update from SDK handler.");
    }

    private static void SdkHandlerOnGameConnected(object? sender, EventArgs e)
    {
        Console.WriteLine("SDK connection");
    }

    private void IcueServerOnGameDisconnected(object? sender, GameHandler handler)
    {
        Console.WriteLine("Game has disconnected");
        var gsiHandler = handler.GsiHandler;
        handler.GamePipeConnected -= HandlerOnGamePipeConnected;
        handler.SdkHandler.ColorsUpdated -= SdkHandlerOnColorsUpdated;
        gsiHandler.StateAdded -= GsiHandlerOnStateAdded;
        gsiHandler.EventAdded -= GsiHandlerOnEventAdded;
        
        // print states and events
        Console.WriteLine("Received States:");
        foreach (var state in States)
        {
            Console.WriteLine($"- {state}");
        }

        Console.WriteLine("Received Events:");
        foreach (var gameEvent in Events)
        {
            Console.WriteLine($"- {gameEvent}");
        }

    }
}