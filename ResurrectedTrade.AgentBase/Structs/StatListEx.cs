using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.AgentBase.Structs
{
    public class StatListEx : MemoryReadable<D2StatListExStrc>
    {
        private static readonly uint[] Masks =
        {
            0x00000001, 0x00000002, 0x00000004, 0x00000008, 0x00000010, 0x00000020, 0x00000040, 0x00000080,
            0x00000100, 0x00000200, 0x00000400, 0x00000800, 0x00001000, 0x00002000, 0x00004000, 0x00008000,
            0x00010000, 0x00020000, 0x00040000, 0x00080000, 0x00100000, 0x00200000, 0x00400000, 0x00800000,
            0x01000000, 0x02000000, 0x04000000, 0x08000000, 0x10000000, 0x20000000, 0x40000000, 0x80000000
        };

        public StatListEx(MemoryAccess access, Ptr address) : base(access, address)
        {
        }

        public uint Flags => Struct.BaseStatList.Flags;

        public IReadOnlyList<D2StatStrc> BaseStats =>
            Read<D2StatStrc>(
                Struct.BaseStatList.Stats.pStat,
                Convert.ToUInt32(Struct.BaseStatList.Stats.Size)
            ).ToList();

        public IReadOnlyList<D2StatStrc> FullStats =>
            Read<D2StatStrc>(Struct.FullStats.pStat, Convert.ToUInt32(Struct.FullStats.Size)).ToList();

        public StatListEx MyStats => ReadWrapped<StatListEx>(Struct.pMyStats);

        public StatListEx PrevLink => ReadWrapped<StatListEx>(Struct.pPrevLink);

        public StatListEx LastList => ReadWrapped<StatListEx>(Struct.pLastList);

        public bool HasState(int state)
        {
            return (Struct.StateFlags[state >> 5] & Masks[state & 31]) > 0;
        }

        public StatListEx GetAddedStatsList(int flags = 0x40)
        {
            if ((Flags & 0x80000000) == 0)
            {
                return null;
            }

            StatListEx statList;
            if ((flags & 0x2000) != 0)
            {
                statList = MyStats;
            }
            else
            {
                statList = LastList;
            }

            if (statList == null)
            {
                return null;
            }

            var attempts = 0;
            while ((flags & statList.Flags & 0xFFFFDFFF) == 0)
            {
                if (attempts++ == 10) return null;
                statList = statList.PrevLink;
                if (statList == null)
                {
                    return null;
                }
            }

            return statList;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct D2StatListExStrc
    {
        [FieldOffset(0x0)] public readonly D2StatListStrc BaseStatList; // Without item modifiers
        [FieldOffset(0x48)] public readonly Ptr pPrevLink;
        [FieldOffset(0x68)] public readonly Ptr pLastList;
        [FieldOffset(0x70)] public readonly Ptr pMyStats;
        [FieldOffset(0x80)] public readonly D2StatsArrayStrc FullStats; // With item modifiers

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        [FieldOffset(0xAC8)]
        public readonly uint[] StateFlags;
    }
}
