using iCUE_ReverseEngineer.Icue.Data;

namespace iCUE_ReverseEngineer.Icue.Gsi;

public sealed class IcueStateEventArgs(string stateName) : EventArgs
{
    public string StateName { get; } = stateName;
}

public sealed class GsiHandler
{
    public event EventHandler<IcueStateEventArgs>? StateAdded;
    public event EventHandler<IcueStateEventArgs>? StateRemoved;
    public event EventHandler<IcueStateEventArgs>? EventAdded;
    public event EventHandler? StatesCleared;
    public event EventHandler? EventsCleared;

    internal Dictionary<string, Action<IcueGameMessage>> GameHandles { get; }

    private readonly IcueToGameConnection _gameConnection;

    internal GsiHandler(IcueToGameConnection gameConnection)
    {
        _gameConnection = gameConnection;

        GameHandles = new Dictionary<string, Action<IcueGameMessage>>
        {
            { "CgSdkPerfromProtocolHandshake", ProtocolHandshake },
            { "CgSdkRequestControl", RequestControl },
            { "CgSdkSetState", CgSdkSetState },
            { "CgSdkSetEvent", CgSdkSetEvent },
            { "CgSdkSetGame", RespondOk },
            { "CgSdkClearState", CgSdkClearState },
            { "CgSdkClearAllEvents", CgSdkClearAllEvents },
            { "CgSdkClearAllStates", CgSdkClearAllStates },
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
            StateAdded?.Invoke(this, new IcueStateEventArgs(stateName));
        }

        RespondOk(message);
    }
    
    private void CgSdkClearState(IcueGameMessage message)
    {
        var stateName = message.Params?.Name;
        if (stateName != null)
        {
            StateRemoved?.Invoke(this, new IcueStateEventArgs(stateName));
        }

        RespondOk(message);
    }
    
    private void CgSdkClearAllStates(IcueGameMessage message)
    {
        StatesCleared?.Invoke(this, EventArgs.Empty);
        RespondOk(message);
    }

    private void CgSdkSetEvent(IcueGameMessage message)
    {
        var eventName = message.Params?.Name;
        if (eventName != null)
        {
            EventAdded?.Invoke(this, new IcueStateEventArgs(eventName));
        }

        RespondOk(message);
    }
    
    private void CgSdkClearAllEvents(IcueGameMessage message)
    {
        EventsCleared?.Invoke(this, EventArgs.Empty);
        RespondOk(message);
    }

    private void RespondOk(IcueGameMessage message)
    {
        const string setGameResponse = """{"result":true,"errorCode":0}""";
        _gameConnection.SendGameMessage(setGameResponse);
    }
}