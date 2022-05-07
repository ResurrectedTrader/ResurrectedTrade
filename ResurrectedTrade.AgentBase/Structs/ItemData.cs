using System.Runtime.InteropServices;
using ResurrectedTrade.AgentBase.Memory;
using ResurrectedTrade.Common.Enums;

namespace ResurrectedTrade.AgentBase.Structs
{
    public class ItemData : MemoryReadable<D2ItemDataStrc>
    {
        public ItemData(MemoryAccess access, Ptr address) : base(access, address)
        {
        }

        public ItemQuality Quality => Struct.Quality;
        public ItemFlags Flags => Struct.ItemFlags;
        public int FileIndex => Struct.FileIndex;

        public short Prefix1 => Struct.Prefix1;
        public short Prefix2 => Struct.Prefix2;
        public short Prefix3 => Struct.Prefix3;

        public short Suffix1 => Struct.Suffix1;
        public short Suffix2 => Struct.Suffix2;
        public short Suffix3 => Struct.Suffix3;
        public short RarePrefix => Struct.RarePrefix;
        public short RareSuffix => Struct.RareSuffix;
        public short AutoAffix => Struct.AutoAffix;
        public byte InvGfxIdx => Struct.InvGfxIdx;
        public Unit NextItem => ReadWrapped<Unit>(Struct.pExtraData.pNextItem);
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct D2ItemDataStrc
    {
        [FieldOffset(0x00)] public readonly ItemQuality Quality;
        [FieldOffset(0x18)] public readonly ItemFlags ItemFlags;
        [FieldOffset(0x34)] public readonly int FileIndex;

        [FieldOffset(0x42)] public readonly short RarePrefix;
        [FieldOffset(0x44)] public readonly short RareSuffix;
        [FieldOffset(0x46)] public readonly short AutoAffix;

        [FieldOffset(0x48)] public readonly short Prefix1;
        [FieldOffset(0x4A)] public readonly short Prefix2;
        [FieldOffset(0x4C)] public readonly short Prefix3;

        [FieldOffset(0x4E)] public readonly short Suffix1;
        [FieldOffset(0x50)] public readonly short Suffix2;
        [FieldOffset(0x52)] public readonly short Suffix3;

        [FieldOffset(0x5E)] public readonly byte InvGfxIdx;

        [FieldOffset(0x70 + 0x30)] public readonly D2ItemExtraDataStrc pExtraData;
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct D2ItemExtraDataStrc
    {
        [FieldOffset(0x10)] public readonly Ptr pNextItem;
    }
}
