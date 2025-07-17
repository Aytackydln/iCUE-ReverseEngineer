namespace iCUE_ReverseEngineer.Client;

public interface IIcueHandler
{
    public Task Start();
}

public class GsiGameHandle(Func<string, bool> isMatch, Func<string, Task> doHandle)
{
    public Func<string, bool> IsMatch { get; } = isMatch;
    public Func<string, Task> DoHandle { get; } = doHandle;
}