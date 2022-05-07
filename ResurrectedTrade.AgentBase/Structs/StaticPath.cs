using System.Drawing;
using System.Runtime.InteropServices;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.AgentBase.Structs
{
    public class StaticPath : MemoryReadable<D2StaticPathStrc>
    {
        public StaticPath(MemoryAccess access, Ptr address) : base(access, address)
        {
        }

        public Point GetPosition()
        {
            return new Point((int)Struct.PosX, (int)Struct.PosY);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct D2StaticPathStrc
    {
        [FieldOffset(0x10)] public readonly uint PosX;
        [FieldOffset(0x14)] public readonly uint PosY;
    }
}
