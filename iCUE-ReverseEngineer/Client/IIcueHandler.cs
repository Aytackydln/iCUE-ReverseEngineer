namespace iCUE_ReverseEngineer.Client;

internal interface IIcueHandler
{
    internal Task Start();
}

internal class GsiGameHandle(Func<string, bool> isMatch, Func<string, Task> doHandle)
{
    internal Func<string, bool> IsMatch { get; } = isMatch;
    internal Func<string, Task> DoHandle { get; } = doHandle;
}