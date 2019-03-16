using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Experimental.Rendering
{
    public interface IBitArray
    {
        uint capacity { get; }
        bool allFalse { get; }
        bool allTrue { get; }
        bool this[uint index] { get; set; }
        string humanizedData { get; }

        IBitArray BitAnd(IBitArray other);
        IBitArray BitOr(IBitArray other);
        IBitArray BitNot();
    }

    // /!\ Important for serialization:
    // Serialization helper will rely on the name of the struct type.
    // In order to work, it must be BitArrayN where N is the capacity without suffix.

    [Serializable]
    [System.Diagnostics.DebuggerDisplay("{this.GetType().Name} {humanizedData}")]
    public struct BitArray8 : IBitArray
    {
        [SerializeField]
        byte data;

        public uint capacity => 8u;
        public bool allFalse => data == 0u;
        public bool allTrue => data == byte.MaxValue;
        public string humanizedData => String.Format("{0, " + capacity + "}", Convert.ToString(data, 2)).Replace(' ', '0');

        public bool this[uint index]
        {
            get => BitArrayUtilities.Get8(index, data);
            set => BitArrayUtilities.Set8(index, ref data, value);
        }

        public BitArray8(byte initValue) => data = initValue;
        public BitArray8(IEnumerable<uint> bitIndexTrue)
        {
            data = (byte)0u;
            if (bitIndexTrue == null)
                return;
            for (int index = bitIndexTrue.Count() - 1; index >= 0; --index)
            {
                uint bitIndex = bitIndexTrue.ElementAt(index);
                if (bitIndex >= capacity) continue;
                data |= (byte)(1u << (int)bitIndex);
            }
        }

        public static BitArray8 operator ~(BitArray8 a) => new BitArray8((byte)~a.data);
        public static BitArray8 operator |(BitArray8 a, BitArray8 b) => new BitArray8((byte)(a.data | b.data));
        public static BitArray8 operator &(BitArray8 a, BitArray8 b) => new BitArray8((byte)(a.data & b.data));

        public IBitArray BitAnd(IBitArray other) => this & (BitArray8)other;
        public IBitArray BitOr(IBitArray other) => this | (BitArray8)other;
        public IBitArray BitNot() => ~this;

        public static bool operator ==(BitArray8 a, BitArray8 b) => a.data == b.data;
        public static bool operator !=(BitArray8 a, BitArray8 b) => a.data != b.data;
        public override bool Equals(object obj) => obj is BitArray8 && ((BitArray8)obj).data == data;
        public override int GetHashCode() => 1768953197 + data.GetHashCode();
    }

    [Serializable]
    [System.Diagnostics.DebuggerDisplay("{this.GetType().Name} {humanizedData}")]
    public struct BitArray16 : IBitArray
    {
        [SerializeField]
        ushort data;

        public uint capacity => 16u;
        public bool allFalse => data == 0u;
        public bool allTrue => data == ushort.MaxValue;
        public string humanizedData => System.Text.RegularExpressions.Regex.Replace(String.Format("{0, " + capacity + "}", Convert.ToString(data, 2)).Replace(' ', '0'), ".{8}", "$0.").TrimEnd('.');
        
        public bool this[uint index]
        {
            get => BitArrayUtilities.Get16(index, data);
            set => BitArrayUtilities.Set16(index, ref data, value);
        }

        public BitArray16(ushort initValue) => data = initValue;
        public BitArray16(IEnumerable<uint> bitIndexTrue)
        {
            data = (ushort)0u;
            if (bitIndexTrue == null)
                return;
            for (int index = bitIndexTrue.Count() - 1; index >= 0; --index)
            {
                uint bitIndex = bitIndexTrue.ElementAt(index);
                if (bitIndex >= capacity) continue;
                data |= (ushort)(1u << (int)bitIndex);
            }
        }

        public static BitArray16 operator ~(BitArray16 a) => new BitArray16((ushort)~a.data);
        public static BitArray16 operator |(BitArray16 a, BitArray16 b) => new BitArray16((ushort)(a.data | b.data));
        public static BitArray16 operator &(BitArray16 a, BitArray16 b) => new BitArray16((ushort)(a.data & b.data));

        public IBitArray BitAnd(IBitArray other) => this & (BitArray16)other;
        public IBitArray BitOr(IBitArray other) => this | (BitArray16)other;
        public IBitArray BitNot() => ~this;

        public static bool operator ==(BitArray16 a, BitArray16 b) => a.data == b.data;
        public static bool operator !=(BitArray16 a, BitArray16 b) => a.data != b.data;
        public override bool Equals(object obj) => obj is BitArray16 && ((BitArray16)obj).data == data;
        public override int GetHashCode() => 1768953197 + data.GetHashCode();
    }

    [Serializable]
    [System.Diagnostics.DebuggerDisplay("{this.GetType().Name} {humanizedData}")]
    public struct BitArray32 : IBitArray
    {
        [SerializeField]
        uint data;

        public uint capacity => 32u;
        public bool allFalse => data == 0u;
        public bool allTrue => data == uint.MaxValue;
        string humanizedVersion => Convert.ToString(data, 2);
        public string humanizedData => System.Text.RegularExpressions.Regex.Replace(String.Format("{0, " + capacity + "}", Convert.ToString(data, 2)).Replace(' ', '0'), ".{8}", "$0.").TrimEnd('.');

        public bool this[uint index]
        {
            get => BitArrayUtilities.Get32(index, data);
            set => BitArrayUtilities.Set32(index, ref data, value);
        }

        public BitArray32(uint initValue) => data = initValue;
        public BitArray32(IEnumerable<uint> bitIndexTrue)
        {
            data = 0u;
            if (bitIndexTrue == null)
                return;
            for (int index = bitIndexTrue.Count() - 1; index >= 0; --index)
            {
                uint bitIndex = bitIndexTrue.ElementAt(index);
                if (bitIndex >= capacity) continue;
                data |= 1u << (int)bitIndex;
            }
        }

        public IBitArray BitAnd(IBitArray other) => this & (BitArray32)other;
        public IBitArray BitOr(IBitArray other) => this | (BitArray32)other;
        public IBitArray BitNot() => ~this;

        public static BitArray32 operator ~(BitArray32 a) => new BitArray32(~a.data);
        public static BitArray32 operator |(BitArray32 a, BitArray32 b) => new BitArray32(a.data | b.data);
        public static BitArray32 operator &(BitArray32 a, BitArray32 b) => new BitArray32(a.data & b.data);

        public static bool operator ==(BitArray32 a, BitArray32 b) => a.data == b.data;
        public static bool operator !=(BitArray32 a, BitArray32 b) => a.data != b.data;
        public override bool Equals(object obj) => obj is BitArray32 && ((BitArray32)obj).data == data;
        public override int GetHashCode() => 1768953197 + data.GetHashCode();
    }

    [Serializable]
    [System.Diagnostics.DebuggerDisplay("{this.GetType().Name} {humanizedData}")]
    public struct BitArray64 : IBitArray
    {
        [SerializeField]
        ulong data;

        public uint capacity => 64u;
        public bool allFalse => data == 0uL;
        public bool allTrue => data == ulong.MaxValue;
        public string humanizedData => System.Text.RegularExpressions.Regex.Replace(String.Format("{0, " + capacity + "}", Convert.ToString((long)data, 2)).Replace(' ', '0'), ".{8}", "$0.").TrimEnd('.');

        public bool this[uint index]
        {
            get => BitArrayUtilities.Get64(index, data);
            set => BitArrayUtilities.Set64(index, ref data, value);
        }

        public BitArray64(ulong initValue) => data = initValue;
        public BitArray64(IEnumerable<uint> bitIndexTrue)
        {
            data = 0L;
            if (bitIndexTrue == null)
                return;
            for (int index = bitIndexTrue.Count() - 1; index >= 0; --index)
            {
                uint bitIndex = bitIndexTrue.ElementAt(index);
                if (bitIndex >= capacity) continue;
                data |= 1uL << (int)bitIndex;
            }
        }
        
        public static BitArray64 operator ~(BitArray64 a) => new BitArray64(~a.data);
        public static BitArray64 operator |(BitArray64 a, BitArray64 b) => new BitArray64(a.data | b.data);
        public static BitArray64 operator &(BitArray64 a, BitArray64 b) => new BitArray64(a.data & b.data);

        public IBitArray BitAnd(IBitArray other) => this & (BitArray64)other;
        public IBitArray BitOr(IBitArray other) => this | (BitArray64)other;
        public IBitArray BitNot() => ~this;

        public static bool operator ==(BitArray64 a, BitArray64 b) => a.data == b.data;
        public static bool operator !=(BitArray64 a, BitArray64 b) => a.data != b.data;
        public override bool Equals(object obj) => obj is BitArray64 && ((BitArray64)obj).data == data;
        public override int GetHashCode() => 1768953197 + data.GetHashCode();
    }

    [Serializable]
    [System.Diagnostics.DebuggerDisplay("{this.GetType().Name} {humanizedData}")]
    public struct BitArray128 : IBitArray
    {
        [SerializeField]
        ulong data1;
        [SerializeField]
        ulong data2;

        public uint capacity => 128u;
        public bool allFalse => data1 == 0uL && data2 == 0uL;
        public bool allTrue => data1 == ulong.MaxValue && data2 == ulong.MaxValue;
        public string humanizedData =>
            System.Text.RegularExpressions.Regex.Replace(String.Format("{0, " + 64u + "}", Convert.ToString((long)data2, 2)).Replace(' ', '0'), ".{8}", "$0.")
            + System.Text.RegularExpressions.Regex.Replace(String.Format("{0, " + 64u + "}", Convert.ToString((long)data1, 2)).Replace(' ', '0'), ".{8}", "$0.").TrimEnd('.');

        public bool this[uint index]
        {
            get => BitArrayUtilities.Get128(index, data1, data2);
            set => BitArrayUtilities.Set128(index, ref data1, ref data2, value);
        }

        public BitArray128(ulong initValue1, ulong initValue2)
        {
            data1 = initValue1;
            data2 = initValue2;
        }
        public BitArray128(IEnumerable<uint> bitIndexTrue)
        {
            data1 = data2 = 0uL;
            if (bitIndexTrue == null)
                return;
            for (int index = bitIndexTrue.Count() - 1; index >= 0; --index)
            {
                uint bitIndex = bitIndexTrue.ElementAt(index);
                if (bitIndex < 64u)
                    data1 |= 1uL << (int)bitIndex;
                else if (bitIndex < capacity)
                    data2 |= 1uL << (int)(bitIndex - 64u);
            }
        }

        public static BitArray128 operator ~(BitArray128 a) => new BitArray128(~a.data1, ~a.data2);
        public static BitArray128 operator |(BitArray128 a, BitArray128 b) => new BitArray128(a.data1 | b.data1, a.data2 | b.data2);
        public static BitArray128 operator &(BitArray128 a, BitArray128 b) => new BitArray128(a.data1 & b.data1, a.data2 & b.data2);

        public IBitArray BitAnd(IBitArray other) => this & (BitArray128)other;
        public IBitArray BitOr(IBitArray other) => this | (BitArray128)other;
        public IBitArray BitNot() => ~this;

        public static bool operator ==(BitArray128 a, BitArray128 b) => a.data1 == b.data1 && a.data2 == b.data2;
        public static bool operator !=(BitArray128 a, BitArray128 b) => a.data1 != b.data1 || a.data2 != b.data2;
        public override bool Equals(object obj) => (obj is BitArray128) && data1.Equals(((BitArray128)obj).data1) && data2.Equals(((BitArray128)obj).data2);
        public override int GetHashCode()
        {
            var hashCode = 1755735569;
            hashCode = hashCode * -1521134295 + data1.GetHashCode();
            hashCode = hashCode * -1521134295 + data2.GetHashCode();
            return hashCode;
        }
    }

    [Serializable]
    [System.Diagnostics.DebuggerDisplay("{this.GetType().Name} {humanizedData}")]
    public struct BitArray256 : IBitArray
    {
        [SerializeField]
        ulong data1;
        [SerializeField]
        ulong data2;
        [SerializeField]
        ulong data3;
        [SerializeField]
        ulong data4;

        public uint capacity => 256u;
        public bool allFalse => data1 == 0uL && data2 == 0uL && data3 == 0uL && data4 == 0uL;
        public bool allTrue => data1 == ulong.MaxValue && data2 == ulong.MaxValue && data3 == ulong.MaxValue && data4 == ulong.MaxValue;
        public string humanizedData =>
            System.Text.RegularExpressions.Regex.Replace(String.Format("{0, " + 64u + "}", Convert.ToString((long)data4, 2)).Replace(' ', '0'), ".{8}", "$0.")
            + System.Text.RegularExpressions.Regex.Replace(String.Format("{0, " + 64u + "}", Convert.ToString((long)data3, 2)).Replace(' ', '0'), ".{8}", "$0.")
            + System.Text.RegularExpressions.Regex.Replace(String.Format("{0, " + 64u + "}", Convert.ToString((long) data2, 2)).Replace(' ', '0'), ".{8}", "$0.")
            + System.Text.RegularExpressions.Regex.Replace(String.Format("{0, " + 64u + "}", Convert.ToString((long) data1, 2)).Replace(' ', '0'), ".{8}", "$0.").TrimEnd('.');

        public bool this[uint index]
        {
            get => BitArrayUtilities.Get256(index, data1, data2, data3, data4);
            set => BitArrayUtilities.Set256(index, ref data1, ref data2, ref data3, ref data4, value);
        }

        public BitArray256(ulong initValue1, ulong initValue2, ulong initValue3, ulong initValue4)
        {
            data1 = initValue1;
            data2 = initValue2;
            data3 = initValue3;
            data4 = initValue4;
        }
        public BitArray256(IEnumerable<uint> bitIndexTrue)
        {
            data1 = data2 = data3 = data4 = 0uL;
            if (bitIndexTrue == null)
                return;
            for (int index = bitIndexTrue.Count() - 1; index >= 0; --index)
            {
                uint bitIndex = bitIndexTrue.ElementAt(index);
                if (bitIndex < 64u)
                    data1 |= 1uL << (int)bitIndex;
                else if (bitIndex < 128u)
                    data2 |= 1uL << (int)(bitIndex - 64u);
                else if (bitIndex < 192u)
                    data3 |= 1uL << (int)(bitIndex - 128u);
                else if (bitIndex < capacity)
                    data4 |= 1uL << (int)(bitIndex - 192u);
            }
        }

        public static BitArray256 operator ~(BitArray256 a) => new BitArray256(~a.data1, ~a.data2, ~a.data3, ~a.data4);
        public static BitArray256 operator |(BitArray256 a, BitArray256 b) => new BitArray256(a.data1 | b.data1, a.data2 | b.data2, a.data3 | b.data3, a.data4 | b.data4);
        public static BitArray256 operator &(BitArray256 a, BitArray256 b) => new BitArray256(a.data1 & b.data1, a.data2 & b.data2, a.data3 & b.data3, a.data4 & b.data4);

        public IBitArray BitAnd(IBitArray other) => this & (BitArray256)other;
        public IBitArray BitOr(IBitArray other) => this | (BitArray256)other;
        public IBitArray BitNot() => ~this;

        public static bool operator ==(BitArray256 a, BitArray256 b) => a.data1 == b.data1 && a.data2 == b.data2 && a.data3 == b.data3 && a.data4 == b.data4;
        public static bool operator !=(BitArray256 a, BitArray256 b) => a.data1 != b.data1 || a.data2 != b.data2 || a.data3 != b.data3 || a.data4 != b.data4;
        public override bool Equals(object obj) =>
            (obj is BitArray256)
            && data1.Equals(((BitArray256)obj).data1)
            && data2.Equals(((BitArray256)obj).data2)
            && data3.Equals(((BitArray256)obj).data3)
            && data4.Equals(((BitArray256)obj).data4);
        public override int GetHashCode()
        {
            var hashCode = 1870826326;
            hashCode = hashCode * -1521134295 + data1.GetHashCode();
            hashCode = hashCode * -1521134295 + data2.GetHashCode();
            hashCode = hashCode * -1521134295 + data3.GetHashCode();
            hashCode = hashCode * -1521134295 + data4.GetHashCode();
            return hashCode;
        }
    }



    public static class BitArrayUtilities
    {
        //written here to not duplicate the serialized accessor and runtime accessor
        public static bool Get8(uint index, byte data) => (data & (1u << (int)index)) != 0u;
        public static bool Get16(uint index, ushort data) => (data & (1u << (int)index)) != 0u;
        public static bool Get32(uint index, uint data) => (data & (1u << (int)index)) != 0u;
        public static bool Get64(uint index, ulong data) => (data & (1uL << (int)index)) != 0uL;
        public static bool Get128(uint index, ulong data1, ulong data2)
            => index < 64u
            ? (data1 & (1uL << (int)index)) != 0uL
            : (data2 & (1uL << (int)(index - 64u))) != 0uL;
        public static bool Get256(uint index, ulong data1, ulong data2, ulong data3, ulong data4)
            => index < 128u
            ? index < 64u
                ? (data1 & (1uL << (int)index)) != 0uL
                : (data2 & (1uL << (int)(index - 64u))) != 0uL
            : index < 192u
                ? (data3 & (1uL << (int)(index - 128u))) != 0uL
                : (data4 & (1uL << (int)(index - 192u))) != 0uL;
        public static void Set8(uint index, ref byte data, bool value) => data = (byte)(value ? (data | (1u << (int)index)) : (data & ~(1u << (int)index)));
        public static void Set16(uint index, ref ushort data, bool value) => data = (ushort)(value ? (data | (1u << (int)index)) : (data & ~(1u << (int)index)));
        public static void Set32(uint index, ref uint data, bool value) => data = (value ? (data | (1u << (int)index)) : (data & ~(1u << (int)index)));
        public static void Set64(uint index, ref ulong data, bool value) => data = (value ? (data | (1uL << (int)index)) : (data & ~(1uL << (int)index)));
        public static void Set128(uint index, ref ulong data1, ref ulong data2, bool value)
        {
            if (index < 64u)
                data1 = (value ? (data1 | (1uL << (int)index)) : (data1 & ~(1uL << (int)index)));
            else
                data2 = (value ? (data2 | (1uL << (int)(index - 64u))) : (data2 & ~(1uL << (int)(index - 64u))));
        }
        public static void Set256(uint index, ref ulong data1, ref ulong data2, ref ulong data3, ref ulong data4, bool value)
        {
            if (index < 64u)
                data1 = (value ? (data1 | (1uL << (int)index)) : (data1 & ~(1uL << (int)index)));
            else if (index < 128u)
                data2 = (value ? (data2 | (1uL << (int)(index - 64u))) : (data2 & ~(1uL << (int)(index - 64u))));
            else if (index < 192u)
                data3 = (value ? (data3 | (1uL << (int)(index - 64u))) : (data3 & ~(1uL << (int)(index - 128u))));
            else
                data4 = (value ? (data4 | (1uL << (int)(index - 64u))) : (data4 & ~(1uL << (int)(index - 192u))));
        }
    }
}
