using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.AgentBase.Structs
{
    public class Inventory : MemoryReadable<D2InventoryStrc>, IEnumerable<Grid>
    {
        public Inventory(MemoryAccess access, Ptr address) : base(access, address)
        {
        }

        public uint GridCount => Struct.nGridCount;

        public Unit FirstItem => ReadWrapped<Unit>(Struct.pFirstItem);

        public IEnumerator<Grid> GetEnumerator()
        {
            for (var i = 0; i < GridCount; i++)
            {
                var grid = GetGrid((Enums.Grid)i);
                if (grid != null)
                {
                    yield return grid;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Grid GetGrid(Enums.Grid type)
        {
            if ((int)type >= GridCount) return null;
            return ReadWrapped<Grid>(Struct.pGrids + Marshal.SizeOf<D2GridStrc>() * (int)type);
        }

        public IEnumerable<uint> GetSharedStashUnitIDs()
        {
            var current = Struct.pFirstSharedStash;
            while (current != Ptr.Zero)
            {
                var stash = Read<D2SharedStashStrc>(current);
                yield return stash.UnitId;
                current = stash.pNext;
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct D2InventoryStrc
    {
        [FieldOffset(0x10)] public readonly Ptr pFirstItem;
        [FieldOffset(0x20)] public readonly Ptr pGrids;
        [FieldOffset(0x28)] public readonly uint nGridCount;
        [FieldOffset(0x40)] public readonly Ptr pCursorItem;

        [FieldOffset(0x68)] public readonly Ptr pFirstSharedStash;
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct D2SharedStashStrc
    {
        [FieldOffset(0x00)] public readonly uint UnitId;
        [FieldOffset(0x10)] public readonly Ptr pNext;
    }
}
