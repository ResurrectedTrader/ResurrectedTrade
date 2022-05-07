using System.Runtime.InteropServices;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.AgentBase.Structs
{
    public class PlayerData : MemoryReadable<D2PlayerDataStrc>
    {
        public PlayerData(MemoryAccess access, Ptr address) : base(access, address)
        {
        }

        public string Name => Struct.Name;
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct D2PlayerDataStrc
    {
        [FieldOffset(0x00)]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public readonly string Name;
    }
}