using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ResurrectedTrade.AgentBase.Memory
{
    public abstract class MemoryAccess : IDisposable
    {
        public abstract Ptr BaseAddress { get; }

        public abstract void Dispose();

        protected abstract bool ReadMemory(Ptr address, ref byte[] buffer, int size);

        public T Read<T>(Ptr address) where T : struct
        {
            return Read<T>(address, 1)[0];
        }

        public T[] Read<T>(Ptr address, uint count) where T : struct
        {
            if (count == 0)
            {
                return Array.Empty<T>();
            }

            int sz = Marshal.SizeOf<T>();
            var buf = new byte[sz * count];

            if (!ReadMemory(address, ref buf, buf.Length)) throw new IOException("Failed to read");

            return ToStruct<T>(buf);
        }

        public static T[] ToStruct<T>(byte[] buf) where T : struct
        {
            int sz = Marshal.SizeOf<T>();
            if (buf.Length % sz != 0)
            {
                throw new ApplicationException("Buffer size does not match struct size");
            }

            int count = buf.Length / sz;
            T[] result = new T[count];

            // Optimisation when reading byte sized things.
            if (sz == 1)
            {
                Buffer.BlockCopy(buf, 0, result, 0, buf.Length);
                return result;
            }

            var handle = GCHandle.Alloc(buf, GCHandleType.Pinned);
            try
            {
                for (var i = 0; i < count; i++)
                {
                    result[i] = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject() + i * sz, typeof(T));
                }

                return result;
            }
            finally
            {
                handle.Free();
            }
        }
    }
}