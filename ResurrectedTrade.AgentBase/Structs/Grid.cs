using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.AgentBase.Structs
{
    public class Grid : MemoryReadable<D2GridStrc>, IEnumerable<Unit>
    {
        public Grid(MemoryAccess access, Ptr address) : base(access, address)
        {
        }

        public uint Columns => Struct.Columns;

        public uint Rows => Struct.Rows;

        public uint Capacity => Rows * Columns;

        public IEnumerator<Unit> GetEnumerator()
        {
            return Read<Ptr>(Struct.pItems, Capacity)
                .Distinct()
                .Where(o => o != Ptr.Zero)
                .Select(ReadWrapped<Unit>)
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct D2GridStrc
    {
        [FieldOffset(0x10)] public readonly byte Columns;
        [FieldOffset(0x11)] public readonly byte Rows;
        [FieldOffset(0x18)] public readonly Ptr pItems;
    }
}
