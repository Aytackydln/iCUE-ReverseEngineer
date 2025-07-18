using System.Runtime.InteropServices;

namespace iCUE_ReverseEngineer;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct IcueLedColor(IcueLedId LedId, byte R, byte G, byte B)
{
    public readonly IcueLedId LedId = LedId;
    public readonly byte B = B;
    public readonly byte G = G;
    public readonly byte R = R;
    
    public IcueColor ToIcueColor() => new IcueColor(R, G, B);
}