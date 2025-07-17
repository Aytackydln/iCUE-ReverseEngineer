namespace iCUE_ReverseEngineer.Icue.Handlers;

public class GsiHandler
{
    public Dictionary<string, Action<IcueGameMessage>> GameHandles { get; }

    private readonly IcueToGameConnection _gameConnection;

    public HashSet<string> States { get; } = [];
    public HashSet<string> Events { get; } = [];

    public GsiHandler(IcueToGameConnection gameConnection)
    {
        _gameConnection = gameConnection;

        GameHandles = new Dictionary<string, Action<IcueGameMessage>>
        {
            { "CgSdkPerfromProtocolHandshake", ProtocolHandshake },
            { "CgSdkRequestControl", RequestControl },
            { "CgSdkSetState", CgSdkSetState },
            { "CgSdkSetEvent", CgSdkSetEvent },
            { "CgSdkSetGame", RespondOk },
            { "CgSdkClearState", RespondOk },
            { "CgSdkClearAllEvents", RespondOk },
            { "CgSdkClearAllStates", RespondOk },
            { "CgSdkReleaseControl", RespondOk },
        };
    }

    private void ProtocolHandshake(IcueGameMessage message)
    {
        const string handshakeOkay = """{"serverProtocolVersion":1,"serverVersion":"5.30.90","breakingChanges":false}""";
        _gameConnection.SendGameMessage(handshakeOkay);
    }

    private void RequestControl(IcueGameMessage message)
    {
        const string controlGranted = """{"result":true}""";
        _gameConnection.SendGameMessage(controlGranted);
    }

    private void CgSdkSetState(IcueGameMessage message)
    {
        var stateName = message.Params?.Name;
        if (stateName != null)
        {
            States.Add(stateName);
        }

        RespondOk(message);
    }

    private void CgSdkSetEvent(IcueGameMessage message)
    {
        var eventName = message.Params?.Name;
        if (eventName != null)
        {
            Events.Add(eventName);
        }

        RespondOk(message);
    }

    private void RespondOk(IcueGameMessage message)
    {
        const string setGameResponse = """{"result":true,"errorCode":0}""";
        _gameConnection.SendGameMessage(setGameResponse);
    }
}