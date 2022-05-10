using System.Runtime.InteropServices;
using System.Text;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.AgentBase.Structs
{
    public class PlayerData : MemoryReadable<D2PlayerDataStrc>
    {
        public PlayerData(MemoryAccess access, Ptr address) : base(access, address)
        {
        }

        public string Name
        {
            get
            {
                var len = 0;
                while (Struct.Name[len] != 0) len++;
                return Encoding.UTF8.GetString(Struct.Name, 0, len);
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct D2PlayerDataStrc
    {
        [FieldOffset(0x00)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly byte[] Name;
    }
}
