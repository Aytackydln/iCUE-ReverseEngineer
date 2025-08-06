using System.Runtime.InteropServices;

namespace iCUE_ReverseEngineer;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly record struct IcueLedColor(IcueLedId LedId, byte R, byte G, byte B)
{
    internal readonly IcueLedId LedId = LedId;
    internal readonly byte B = B;
    internal readonly byte G = G;
    internal readonly byte R = R;
    
    internal IcueColor ToIcueColor() => new(R, G, B);
}