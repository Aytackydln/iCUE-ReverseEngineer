Utilities to act as or interact with an iCUE software for game integration.

Usage:
```csharp
IcueServer icueServer = new();
icueServer.CallbackPipeConnected += IcueServerOnCallbackPipeConnected;
icueServer.OutputConnected += IcueServerOnOutputConnected;
icueServer.GameConnected += IcueServerOnGameConnected;
icueServer.GameDisconnected += IcueServerOnGameDisconnected;
icueServer.Run();
```