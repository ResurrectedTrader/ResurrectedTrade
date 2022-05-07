using System;
using System.Drawing;
using System.Runtime.InteropServices;
using ResurrectedTrade.AgentBase.Enums;
using ResurrectedTrade.AgentBase.Memory;

namespace ResurrectedTrade.AgentBase.Structs
{
    public class Unit : MemoryReadable<D2UnitStrc>, IEquatable<Unit>
    {
        public Unit(MemoryAccess access, Ptr address) : base(access, address)
        {
        }

        public UnitType Type => Struct.dwUnitType;

        public uint ClassId => Struct.dwClassId;

        public uint Mode => Struct.AnimationMode;

        public uint UnitId => Struct.dwUnitId;

        public Inventory Inventory => ReadWrapped<Inventory>(Struct.pInventory);

        public Unit ListNext => ReadWrapped<Unit>(Struct.pListNext);

        public StatListEx StatList => ReadWrapped<StatListEx>(Struct.pStatListEx);

        public Point Position => ReadWrapped<StaticPath>(Struct.pPath)?.GetPosition() ?? Point.Empty;

        public ItemData ItemData => Type == UnitType.Item ? ReadWrapped<ItemData>(Struct.pUnitData) : null;

        public PlayerData PlayerData => Type == UnitType.Player ? ReadWrapped<PlayerData>(Struct.pUnitData) : null;

        public bool Equals(Unit other)
        {
            if (other == null) return false;
            return other.Type == Type && other.UnitId == UnitId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Unit)obj);
        }

        public override int GetHashCode()
        {
            return new { Type, UnitId }.GetHashCode();
        }

        public bool IsFullyLoaded()
        {
            var pRoom = Read<Ptr>(Struct.pPath + 0x20);
            if (pRoom == Ptr.Zero) return false;
            var pRoomEx = Read<Ptr>(pRoom + 0x18);
            if (pRoomEx == Ptr.Zero) return false;
            var pLevel = Read<Ptr>(pRoomEx + 0x90);
            if (pLevel == Ptr.Zero) return false;
            return true;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct D2UnitStrc
    {
        [FieldOffset(0x00)] public readonly UnitType dwUnitType;
        [FieldOffset(0x04)] public readonly uint dwClassId;
        [FieldOffset(0x08)] public readonly uint dwUnitId;
        [FieldOffset(0x0C)] public readonly uint AnimationMode;
        [FieldOffset(0x10)] public readonly Ptr pUnitData;
        [FieldOffset(0x38)] public readonly Ptr pPath;
        [FieldOffset(0x88)] public readonly Ptr pStatListEx;
        [FieldOffset(0x90)] public readonly Ptr pInventory;
        [FieldOffset(0xD8)] public readonly uint UnkSortStashesBy;
        [FieldOffset(0x150)] public readonly Ptr pListNext;
    }
}