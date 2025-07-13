using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace iCUE_ReverseEngineer.Icue;
    
public static class IpcLogger
{
    public static NamedPipeServerStream CreateInPipe(string pipeName)
    {
        Console.WriteLine("Creating pipe: " + pipeName);
        
        var ps = new PipeSecurity();
        ps.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User!, PipeAccessRights.FullControl, AccessControlType.Allow));

        var pipe = NamedPipeServerStreamAcl.Create(
            pipeName,
            PipeDirection.In,                     // PIPE_ACCESS_INBOUND
            maxNumberOfServerInstances: 1,        // nMaxInstances = 1
            transmissionMode: PipeTransmissionMode.Byte,
            options: PipeOptions.Asynchronous,    // FILE_FLAG_OVERLAPPED
            inBufferSize: 4096,
            outBufferSize: 4096,
            pipeSecurity: ps
        );
        
        Console.WriteLine("Waiting for connection on pipe: " + pipe.SafePipeHandle);
        return pipe;
    }

    public static NamedPipeServerStream CreateOutPipe(string pipeName)
    {
        Console.WriteLine("Creating pipe: " + pipeName);
        
        var ps = new PipeSecurity();
        ps.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User!, PipeAccessRights.FullControl, AccessControlType.Allow));

        var pipe = NamedPipeServerStreamAcl.Create(
            pipeName,
            PipeDirection.Out,
            maxNumberOfServerInstances: 1,        // nMaxInstances = 1
            transmissionMode: PipeTransmissionMode.Byte,
            options: PipeOptions.Asynchronous,    // FILE_FLAG_OVERLAPPED
            inBufferSize: 4096,
            outBufferSize: 4096,
            pipeSecurity: ps
        );
        
        Console.WriteLine("Waiting for connection on pipe: " + pipe.SafePipeHandle);
        return pipe;
    }
}