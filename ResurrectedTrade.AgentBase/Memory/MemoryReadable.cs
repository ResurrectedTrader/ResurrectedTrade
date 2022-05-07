using System;

namespace ResurrectedTrade.AgentBase.Memory
{
    public abstract class MemoryReadable<T> where T : struct
    {
        public readonly MemoryAccess Access;
        public readonly Ptr Address;

        public MemoryReadable(MemoryAccess access, Ptr address)
        {
            Access = access;
            Address = address;
            Reload();
        }

        public T Struct { get; private set; }

        public void Reload()
        {
            Struct = Read<T>(Address);
        }

        protected U Read<U>(Ptr addr) where U : struct
        {
            return Access.Read<U>(addr);
        }

        protected U[] Read<U>(Ptr addr, uint count) where U : struct
        {
            return Access.Read<U>(addr, count);
        }

        protected U ReadWrapped<U>(Ptr address) where U : class
        {
            if (address == Ptr.Zero) return null;

            return (U)Activator.CreateInstance(typeof(U), Access, address);
        }
    }
}