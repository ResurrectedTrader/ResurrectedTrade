using System.Runtime.InteropServices;

namespace ResurrectedTrade.AgentBase.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct D2UIStates
    {
        [FieldOffset(0x000)] public readonly bool InGame;
    }
}
