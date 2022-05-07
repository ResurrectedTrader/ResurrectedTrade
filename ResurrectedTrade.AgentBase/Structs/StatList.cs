using System.Runtime.InteropServices;
using ResurrectedTrade.AgentBase.Memory;
using ResurrectedTrade.Common.Enums;

namespace ResurrectedTrade.AgentBase.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct D2StatListStrc
    {
        [FieldOffset(0x1C)] public readonly uint Flags;
        [FieldOffset(0x30)] public readonly D2StatsArrayStrc Stats;
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct D2StatStrc
    {
        [FieldOffset(0x0)] public readonly ushort Layer;
        [FieldOffset(0x2)] public readonly Stat Stat;
        [FieldOffset(0x4)] public readonly int Value;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x18)]
    public readonly struct D2StatsArrayStrc
    {
        [FieldOffset(0x0)] public readonly Ptr pStat;
        [FieldOffset(0x8)] public readonly ulong Size;
    }
}