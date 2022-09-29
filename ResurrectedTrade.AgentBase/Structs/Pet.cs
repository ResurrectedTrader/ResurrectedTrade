using System.Runtime.InteropServices;
using ResurrectedTrade.AgentBase.Enums;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.AgentBase.Structs
{
    public class Pet : MemoryReadable<PetStruct>
    {
        public Pet(MemoryAccess access, Ptr address) : base(access, address)
        {
        }

        public uint OwnerId => Struct.OwnerId;

        public uint UnitId => Struct.UnitId;
        public uint ClassId => Struct.ClassId;

        public Pet GetNext()
        {
            return ReadWrapped<Pet>(Struct.pNextPet);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct PetStruct
    {
        [FieldOffset(0x00)] public readonly uint ClassId;
        [FieldOffset(0x04)] public readonly PetType PetType;
        [FieldOffset(0x08)] public readonly uint UnitId;
        [FieldOffset(0x0C)] public readonly uint OwnerId;
        [FieldOffset(0x30)] public readonly Ptr pNextPet;
    }
}
