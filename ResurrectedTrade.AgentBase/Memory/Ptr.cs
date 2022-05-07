using System;

namespace ResurrectedTrade.AgentBase.Memory
{
    public readonly struct Ptr
    {
        public static readonly int Size = IntPtr.Size;
        public static Ptr Zero = new Ptr(0);

        private readonly long _value;

        public Ptr(IntPtr ptr) : this(ptr.ToInt64())
        {
        }

        public Ptr(long value)
        {
            _value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is Ptr other) return _value == other._value;

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public static bool operator ==(Ptr one, Ptr other)
        {
            return one._value == other._value;
        }

        public static bool operator !=(Ptr one, Ptr other)
        {
            return one._value != other._value;
        }

        public static Ptr operator +(Ptr one, Ptr other)
        {
            return new Ptr(one._value + other._value);
        }

        public static Ptr operator +(Ptr one, int other)
        {
            return new Ptr(one._value + other);
        }

        public static Ptr operator +(Ptr one, long other)
        {
            return new Ptr(one._value + other);
        }

        public static Ptr operator +(Ptr one, uint other)
        {
            return new Ptr(one._value + other);
        }


        public static Ptr operator -(Ptr one, Ptr other)
        {
            return new Ptr(one._value - other._value);
        }

        public static Ptr operator -(Ptr one, int other)
        {
            return new Ptr(one._value - other);
        }

        public static Ptr operator -(Ptr one, long other)
        {
            return new Ptr(one._value - other);
        }

        public static Ptr operator -(Ptr one, uint other)
        {
            return new Ptr(one._value - other);
        }

        public static implicit operator IntPtr(Ptr ptr)
        {
            return new IntPtr(ptr._value);
        }

        public static implicit operator int(Ptr ptr)
        {
            return (int)ptr._value;
        }

        public static implicit operator long(Ptr ptr)
        {
            return ptr._value;
        }

        public static implicit operator ulong(Ptr ptr)
        {
            return unchecked((ulong)ptr._value);
        }

        public static implicit operator uint(Ptr ptr)
        {
            return (uint)ptr._value;
        }

        public static implicit operator Ptr(IntPtr ptr)
        {
            return new Ptr(ptr);
        }

        public static implicit operator Ptr(int value)
        {
            return new Ptr(value);
        }

        public static implicit operator Ptr(long value)
        {
            return new Ptr(value);
        }

        public static implicit operator Ptr(ulong value)
        {
            return new Ptr(unchecked((long)value));
        }

        public static implicit operator Ptr(uint value)
        {
            return new Ptr(value);
        }

        public override string ToString()
        {
            return $"0x{_value:X}";
        }
    }
}
