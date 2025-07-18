using System.Runtime.InteropServices;

namespace iCUE_ReverseEngineer;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct IcueColor(byte R, byte G, byte B)
{
    public readonly byte B = B;
    public readonly byte G = G;
    public readonly byte R = R;
}