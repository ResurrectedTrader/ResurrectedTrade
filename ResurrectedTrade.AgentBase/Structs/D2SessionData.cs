using System.Runtime.InteropServices;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.AgentBase.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct D2SessionData
    {
        [FieldOffset(0x130)] public readonly Ptr pBattleTagStr;
        [FieldOffset(0x138)] public readonly ulong BattleTagLength;
    }
}
