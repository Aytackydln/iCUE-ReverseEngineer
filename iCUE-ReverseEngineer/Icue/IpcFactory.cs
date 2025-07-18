using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace iCUE_ReverseEngineer.Icue;

public static class IpcFactory
{
    public static NamedPipeServerStream CreateInPipe(string pipeName)
    {
        var ps = new PipeSecurity();
        ps.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User!, PipeAccessRights.FullControl, AccessControlType.Allow));

        return NamedPipeServerStreamAcl.Create(
            pipeName,
            PipeDirection.In,
            maxNumberOfServerInstances: 1,
            transmissionMode: PipeTransmissionMode.Byte,
            options: PipeOptions.Asynchronous,
            inBufferSize: 4096,
            outBufferSize: 4096,
            pipeSecurity: ps
        );
    }

    public static NamedPipeServerStream CreateOutPipe(string pipeName)
    {
        var ps = new PipeSecurity();
        ps.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User!, PipeAccessRights.FullControl, AccessControlType.Allow));

        return NamedPipeServerStreamAcl.Create(
            pipeName,
            PipeDirection.Out,
            maxNumberOfServerInstances: 1, // nMaxInstances = 1
            transmissionMode: PipeTransmissionMode.Byte,
            options: PipeOptions.Asynchronous, // FILE_FLAG_OVERLAPPED
            inBufferSize: 4096,
            outBufferSize: 4096,
            pipeSecurity: ps
        );
    }
}