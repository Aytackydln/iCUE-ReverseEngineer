namespace iCUE_ReverseEngineer.Icue.Handlers;

public class SdkHandler
{
    public Dictionary<string, Action<IcueGameMessage>> SdkHandles { get; }

    private readonly IcueToGameConnection _gameConnection;

    public SdkHandler(IcueToGameConnection gameConnection)
    {
        _gameConnection = gameConnection;

        SdkHandles = new Dictionary<string, Action<IcueGameMessage>>
        {
            { "CorsiarHandshakeMethod", Handshake },
            { "CorsairGetDeviceCount", DeviceCount },
            { "CorsairGetDeviceInfo", DeviceInfo },
            { "CorsairGetLedPositions", LedPositions },
            { "CorsairSetLedsColors", RespondOk },
        };
    }

    private void Handshake(IcueGameMessage message)
    {
        const string handshakeOkay = """{"serverProtocolVersion":16,"serverVersion":"5.30.90","breakingChanges":false}""";
        _gameConnection.SendGameMessage(handshakeOkay);
    }

    private void DeviceCount(IcueGameMessage obj)
    {
        const string deviceCountResponse = """{"result":2}""";
        _gameConnection.SendGameMessage(deviceCountResponse);
    }

    private void DeviceInfo(IcueGameMessage message)
    {
        //TODO: Implement a diverse device list and return with the correct device index
        var deviceIndex = message.Params?.DeviceIndex ?? "0";

        switch (deviceIndex)
        {
            case "0":
            {
                const string device1 = """{"result":{"channels":null,"type":2,"ledsCount":117,"logicalLayout":4,"model":"K70 RGB PRO","deviceId":"2de9d9a946059667c6ad4dc8c8a0f30a","physicalLayout":2,"capsMask":1}}""";
                _gameConnection.SendGameMessage(device1);
                break;
            }
            case "1":
            {
                const string device2 = """{"result":{"channels":null,"type":3,"ledsCount":1,"logicalLayout":0,"model":"HS80","deviceId":"a89b3cbe2650299d74bbc71b1eea44f1","physicalLayout":0,"capsMask":3}}""";
                _gameConnection.SendGameMessage(device2);
                break;
            }
        }
    }

    private void LedPositions(IcueGameMessage obj)
    {
        const string ledPositionsResponse = """
                                            {"result":[{"width":13,"ledId":46,"top":68.3,"left":137.9,"height":13},{"ledId":47,"width":13,"top":68.3,"left":147.8,"height":13},{"width":13,"ledId":44,"top":68.3,"left":118.1,"height":13},{"ledId":45,"width":13,"top":68.3,"left":128.1,"height":13},{"width":13,"ledId":42,"top":68.3,"left":98.4,"height":13},{"width":13,"ledId":43,"top":68.3,"left":108.4,"height":13},{"ledId":40,"width":13,"top":68.2,"left":78.7,"height":13},{"ledId":41,"width":13,"top":68.3,"left":88.5,"height":13},{"ledId":38,"width":13,"top":68.3,"left":58.9,"height":13},{"width":13,"ledId":39,"top":68.3,"left":68.8,"height":13},{"width":13,"ledId":36,"top":58.6,"left":155.1,"height":13},{"width":13,"ledId":37,"top":68.3,"left":45.3,"height":13},{"width":13,"ledId":34,"top":58.6,"left":135.5,"height":13},{"ledId":35,"width":13,"top":58.6,"left":145.3,"height":13},{"ledId":32,"width":13,"top":58.6,"left":115.7,"height":13},{"ledId":33,"width":13,"top":58.6,"left":125.5,"height":13},{"ledId":62,"width":13,"top":88.1,"left":55.2,"height":13},{"ledId":63,"width":13,"top":88.1,"left":67.5,"height":13},{"ledId":60,"width":13,"top":78.2,"left":152.6,"height":13},{"width":13,"ledId":61,"top":88.1,"left":42.8,"height":13},{"width":13,"ledId":58,"top":78.2,"left":133,"height":13},{"width":13,"ledId":59,"top":78.2,"left":142.9,"height":13},{"width":13,"ledId":56,"top":78.2,"left":113.2,"height":13},{"width":13,"ledId":57,"top":78.2,"left":123,"height":13},{"width":13,"ledId":54,"top":78.2,"left":93.5,"height":13},{"width":13,"ledId":55,"top":78.2,"left":103.4,"height":13},{"width":13,"ledId":52,"top":78.2,"left":73.6,"height":13},{"width":13,"ledId":53,"top":78.2,"left":83.7,"height":13},{"ledId":50,"width":13,"top":78.2,"left":53.9,"height":13},{"ledId":51,"width":13,"top":78.2,"left":63.9,"height":13},{"ledId":48,"width":13,"top":68.3,"left":157.7,"height":13},{"ledId":49,"width":13,"top":78.1,"left":47.7,"height":13},{"ledId":14,"width":13,"top":48.8,"left":51.3,"height":13},{"ledId":15,"width":13,"top":48.8,"left":61.1,"height":13},{"ledId":12,"width":13,"top":37.9,"left":169.9,"height":13},{"ledId":13,"width":13,"top":48.8,"left":41.5,"height":13},{"ledId":10,"width":13,"top":37.9,"left":150.1,"height":13},{"ledId":11,"width":13,"top":37.9,"left":160,"height":13},{"ledId":8,"width":13,"top":37.9,"left":125.5,"height":13},{"ledId":9,"width":13,"top":37.9,"left":135.5,"height":13},{"ledId":6,"width":13,"top":37.9,"left":105.6,"height":13},{"ledId":7,"width":13,"top":37.9,"left":115.7,"height":13},{"width":13,"ledId":4,"top":37.9,"left":81.1,"height":13},{"width":13,"ledId":5,"top":37.9,"left":90.9,"height":13},{"width":13,"ledId":2,"top":37.9,"left":61.1,"height":13},{"width":13,"ledId":3,"top":37.9,"left":71.2,"height":13},{"width":13,"ledId":1,"top":37.9,"left":41.5,"height":13},{"width":13,"ledId":30,"top":58.6,"left":95.9,"height":13},{"width":13,"ledId":31,"top":58.6,"left":105.6,"height":13},{"width":13,"ledId":28,"top":58.6,"left":76.2,"height":13},{"width":13,"ledId":29,"top":58.6,"left":86,"height":13},{"width":13,"ledId":26,"top":58.6,"left":56.4,"height":13},{"width":13,"ledId":27,"top":58.6,"left":66.3,"height":13},{"width":13,"ledId":24,"top":48.8,"left":150.1,"height":13},{"width":13,"ledId":25,"top":58.6,"left":43.9,"height":13},{"width":13,"ledId":22,"top":48.8,"left":130.4,"height":13},{"width":13,"ledId":23,"top":48.8,"left":140.3,"height":13},{"width":13,"ledId":20,"top":48.8,"left":110.7,"height":13},{"width":13,"ledId":21,"top":48.8,"left":120.5,"height":13},{"width":13,"ledId":18,"top":48.8,"left":90.9,"height":13},{"width":13,"ledId":19,"top":48.8,"left":100.8,"height":13},{"width":13,"ledId":16,"top":48.8,"left":71.2,"height":13},{"width":13,"ledId":17,"top":48.8,"left":81.1,"height":13},{"ledId":108,"width":13,"top":83,"left":254.1,"height":13},{"ledId":109,"width":13,"top":58.6,"left":224.5,"height":13},{"ledId":107,"width":13,"top":63.5,"left":254.1,"height":13},{"ledId":105,"width":13,"top":48.8,"left":244.2,"height":13},{"ledId":106,"width":13,"top":48.8,"left":254.1,"height":13},{"ledId":103,"width":13,"top":48.8,"left":224.5,"height":13},{"ledId":104,"width":13,"top":48.8,"left":234.3,"height":13},{"ledId":101,"width":13,"top":35.3,"left":243.7,"height":13},{"ledId":102,"width":13,"top":35.3,"left":253.1,"height":13},{"ledId":99,"width":13,"top":35.3,"left":224.4,"height":13},{"ledId":100,"width":13,"top":35.3,"left":233.9,"height":13},{"ledId":97,"width":13,"top":21.7,"left":61.4,"height":13},{"ledId":98,"width":13,"top":21.7,"left":233.4,"height":13},{"ledId":95,"width":13,"top":88.1,"left":202,"height":13},{"ledId":96,"width":13,"top":88.1,"left":212,"height":13},{"ledId":120,"width":13,"top":88.1,"left":244.2,"height":13},{"ledId":118,"width":13,"top":78.2,"left":244.2,"height":13},{"ledId":119,"width":13,"top":88.1,"left":229.4,"height":13},{"ledId":116,"width":13,"top":78.2,"left":224.5,"height":13},{"ledId":117,"width":13,"top":78.2,"left":234.3,"height":13},{"ledId":114,"width":13,"top":68.3,"left":234.3,"height":13},{"ledId":115,"width":13,"top":68.3,"left":244.2,"height":13},{"ledId":113,"width":13,"top":68.3,"left":224.5,"height":13},{"ledId":110,"width":13,"top":58.6,"left":234.3,"height":13},{"ledId":111,"width":13,"top":58.6,"left":244.2,"height":13},{"ledId":77,"width":13,"top":48.8,"left":192.2,"height":13},{"ledId":78,"width":13,"top":48.8,"left":202,"height":13},{"ledId":75,"width":13,"top":37.9,"left":202.1,"height":13},{"ledId":76,"width":13,"top":37.9,"left":212,"height":13},{"ledId":74,"width":13,"top":37.9,"left":192.2,"height":13},{"ledId":72,"width":13,"top":21.7,"left":52,"height":13},{"width":13,"ledId":73,"top":37.9,"left":179.8,"height":13},{"width":13,"ledId":70,"top":88.1,"left":166.2,"height":13},{"width":13,"ledId":68,"top":88.1,"left":141.6,"height":13},{"width":13,"ledId":65,"top":88.1,"left":104.6,"height":13},{"width":13,"ledId":93,"top":78.2,"left":202,"height":13},{"width":13,"ledId":94,"top":88.1,"left":192.2,"height":13},{"width":13,"ledId":91,"top":78.2,"left":171.1,"height":13},{"width":13,"ledId":92,"top":88.1,"left":178.6,"height":13},{"width":13,"ledId":89,"top":58.6,"left":202,"height":13},{"width":13,"ledId":90,"top":58.6,"left":212,"height":13},{"width":13,"ledId":87,"top":48.8,"left":174.9,"height":13},{"width":13,"ledId":88,"top":58.6,"left":192.2,"height":13},{"width":13,"ledId":85,"top":48.8,"left":160,"height":13},{"width":13,"ledId":83,"top":63.5,"left":177.3,"height":13},{"width":13,"ledId":82,"top":68.5,"left":173.6,"height":13},{"width":13,"ledId":79,"top":48.8,"left":212,"height":13},{"width":13,"ledId":80,"top":58.6,"left":165,"height":13},{"width":13,"ledId":1543,"top":21.7,"left":42.5,"height":13},{"width":13,"ledId":502,"top":88.1,"left":85.5,"height":13},{"width":13,"ledId":503,"top":88.1,"left":123.6,"height":13},{"width":13,"ledId":147,"top":88.1,"left":153.9,"height":13},{"width":13,"ledId":501,"top":21.4,"left":149.3,"height":13},{"width":13,"ledId":500,"top":21.4,"left":145.7,"height":13}]}
                                            """;
        _gameConnection.SendGameMessage(ledPositionsResponse);
    }

    private void RespondOk(IcueGameMessage message)
    {
        const string setGameResponse = """{"result":true,"errorCode":0}""";
        _gameConnection.SendGameMessage(setGameResponse);
    }
}