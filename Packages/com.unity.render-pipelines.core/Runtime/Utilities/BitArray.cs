using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// IBitArray interface.
    /// </summary>
    public interface IBitArray
    {
        /// <summary>Gets the capacity of this BitArray. This is the number of bits that are usable.</summary>
        uint capacity { get; }
        /// <summary>Return `true` if all the bits of this BitArray are set to 0. Returns `false` otherwise.</summary>
        bool allFalse { get; }
        /// <summary>Return `true` if all the bits of this BitArray are set to 1. Returns `false` otherwise.</summary>
        bool allTrue { get; }
        /// <summary>
        /// An indexer that allows access to the bit at a given index. This provides both read and write access.
        /// </summary>
        /// <param name="index">Index of the bit.</param>
        /// <returns>State of the bit at the provided index.</returns>
        bool this[uint index] { get; set; }
        /// <summary>Writes the bits in the array in a human-readable form. This is as a string of 0s and 1s packed by 8 bits. This is useful for debugging.</summary>
        string humanizedData { get; }

        /// <summary>
        /// Perform an AND bitwise operation between this BitArray and the one you pass into the function and return the result. Both BitArrays must have the same capacity. This will not change current BitArray values.
        /// </summary>
        /// <param name="other">BitArray with which to the And operation.</param>
        /// <returns>The resulting bit array.</returns>
        IBitArray BitAnd(IBitArray other);
        /// <summary>
        /// Perform an OR bitwise operation between this BitArray and the one you pass into the function and return the result. Both BitArrays must have the same capacity. This will not change current BitArray values.
        /// </summary>
        /// <param name="other">BitArray with which to the Or operation.</param>
        /// <returns>The resulting bit array.</returns>
        IBitArray BitOr(IBitArray other);
        /// <summary>
        /// Return the BitArray with every bit inverted.
        /// </summary>
        /// <returns></returns>
        IBitArray BitNot();
    }

    // /!\ Important for serialization:
    // Serialization helper will rely on the name of the struct type.
    // In order to work, it must be BitArrayN where N is the capacity without suffix.

    /// <summary>
    /// Bit array of size 8.
    /// </summary>
    [Serializable]
    [System.Diagnostics.DebuggerDisplay("{this.GetType().Name} {humanizedData}")]
    public struct BitArray8 : IBitArray
    {
        [SerializeField]
        byte data;

        /// <summary>Number of elements in the bit array.</summary>
        public uint capacity => 8u;
        /// <summary>True if all bits are 0.</summary>
        public bool allFalse => data == 0u;
        /// <summary>True if all bits are 1.</summary>
        public bool allTrue => data == byte.MaxValue;
        /// <summary>Returns the bit array in a human readable form.</summary>
        public string humanizedData => String.Format("{0, " + capacity + "}", Convert.ToString(data, 2)).Replace(' ', '0');

        /// <summary>
        /// Returns the state of the bit at a specific index.
        /// </summary>
        /// <param name="index">Index of the bit.</param>
        /// <returns>State of the bit at the provided index.</returns>
        public bool this[uint index]
        {
            get => BitArrayUtilities.Get8(index, data);
            set => BitArrayUtilities.Set8(index, ref data, value);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="initValue">Initialization value.</param>
        public BitArray8(byte initValue) => data = initValue;
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bitIndexTrue">List of indices where bits should be set to true.</param>
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

        /// <summary>
        /// Bit-wise Not operator
        /// </summary>
        /// <param name="a">Bit array with which to do the operation.</param>
        /// <returns>The resulting bit array.</returns>
        public static BitArray8 operator ~(BitArray8 a) => new BitArray8((byte)~a.data);
        /// <summary>
        /// Bit-wise Or operator
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>The resulting bit array.</returns>
        public static BitArray8 operator |(BitArray8 a, BitArray8 b) => new BitArray8((byte)(a.data | b.data));
        /// <summary>
        /// Bit-wise And operator
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>The resulting bit array.</returns>
        public static BitArray8 operator &(BitArray8 a, BitArray8 b) => new BitArray8((byte)(a.data & b.data));

        /// <summary>
        /// Bit-wise And
        /// </summary>
        /// <param name="other">Bit array with which to do the operation.</param>
        /// <returns>The resulting bit array.</returns>
        public IBitArray BitAnd(IBitArray other) => this & (BitArray8)other;
        /// <summary>
        /// Bit-wise Or
        /// </summary>
        /// <param name="other">Bit array with which to do the operation.</param>
        /// <returns>The resulting bit array.</returns>
        public IBitArray BitOr(IBitArray other) => this | (BitArray8)other;
        /// <summary>
        /// Bit-wise Not
        /// </summary>
        /// <returns>The resulting bit array.</returns>
        public IBitArray BitNot() => ~this;

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>True if both bit arrays are equals.</returns>
        public static bool operator ==(BitArray8 a, BitArray8 b) => a.data == b.data;
        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>True if the bit arrays are not equals.</returns>
        public static bool operator !=(BitArray8 a, BitArray8 b) => a.data != b.data;
        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="obj">Bit array to compare to.</param>
        /// <returns>True if the provided bit array is equal to this..</returns>
        public override bool Equals(object obj) => obj is BitArray8 ba8 && ba8.data == data;
        /// <summary>
        /// Get the hashcode of the bit array.
        /// </summary>
        /// <returns>Hashcode of the bit array.</returns>
        public override int GetHashCode() => 1768953197 + data.GetHashCode();
    }

    /// <summary>
    /// Bit array of size 16.
    /// </summary>
    [Serializable]
    [System.Diagnostics.DebuggerDisplay("{this.GetType().Name} {humanizedData}")]
    public struct BitArray16 : IBitArray
    {
        [SerializeField]
        ushort data;

        /// <summary>Number of elements in the bit array.</summary>
        public uint capacity => 16u;
        /// <summary>True if all bits are 0.</summary>
        public bool allFalse => data == 0u;
        /// <summary>True if all bits are 1.</summary>
        public bool allTrue => data == ushort.MaxValue;
        /// <summary>Returns the bit array in a human readable form.</summary>
        public string humanizedData => System.Text.RegularExpressions.Regex.Replace(String.Format("{0, " + capacity + "}", Convert.ToString(data, 2)).Replace(' ', '0'), ".{8}", "$0.").TrimEnd('.');

        /// <summary>
        /// Returns the state of the bit at a specific index.
        /// </summary>
        /// <param name="index">Index of the bit.</param>
        /// <returns>State of the bit at the provided index.</returns>
        public bool this[uint index]
        {
            get => BitArrayUtilities.Get16(index, data);
            set => BitArrayUtilities.Set16(index, ref data, value);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="initValue">Initialization value.</param>
        public BitArray16(ushort initValue) => data = initValue;
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bitIndexTrue">List of indices where bits should be set to true.</param>

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

        /// <summary>
        /// Bit-wise Not operator
        /// </summary>
        /// <param name="a">Bit array with which to do the operation.</param>
        /// <returns>The resulting bit array.</returns>
        public static BitArray16 operator ~(BitArray16 a) => new BitArray16((ushort)~a.data);
        /// <summary>
        /// Bit-wise Or operator
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>The resulting bit array.</returns>
        public static BitArray16 operator |(BitArray16 a, BitArray16 b) => new BitArray16((ushort)(a.data | b.data));
        /// <summary>
        /// Bit-wise And operator
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>The resulting bit array.</returns>
        public static BitArray16 operator &(BitArray16 a, BitArray16 b) => new BitArray16((ushort)(a.data & b.data));

        /// <summary>
        /// Bit-wise And
        /// </summary>
        /// <param name="other">Bit array with which to do the operation.</param>
        /// <returns>The resulting bit array.</returns>
        public IBitArray BitAnd(IBitArray other) => this & (BitArray16)other;
        /// <summary>
        /// Bit-wise Or
        /// </summary>
        /// <param name="other">Bit array with which to do the operation.</param>
        /// <returns>The resulting bit array.</returns>
        public IBitArray BitOr(IBitArray other) => this | (BitArray16)other;
        /// <summary>
        /// Bit-wise Not
        /// </summary>
        /// <returns>The resulting bit array.</returns>
        public IBitArray BitNot() => ~this;

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>True if both bit arrays are equals.</returns>
        public static bool operator ==(BitArray16 a, BitArray16 b) => a.data == b.data;
        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>True if the bit arrays are not equals.</returns>
        public static bool operator !=(BitArray16 a, BitArray16 b) => a.data != b.data;
        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="obj">Bit array to compare to.</param>
        /// <returns>True if the provided bit array is equal to this..</returns>
        public override bool Equals(object obj) => obj is BitArray16 ba16 && ba16.data == data;
        /// <summary>
        /// Get the hashcode of the bit array.
        /// </summary>
        /// <returns>Hashcode of the bit array.</returns>
        public override int GetHashCode() => 1768953197 + data.GetHashCode();
    }

    /// <summary>
    /// Bit array of size 32.
    /// </summary>
    [Serializable]
    [System.Diagnostics.DebuggerDisplay("{this.GetType().Name} {humanizedData}")]
    public struct BitArray32 : IBitArray
    {
        [SerializeField]
        uint data;

        /// <summary>Number of elements in the bit array.</summary>
        public uint capacity => 32u;
        /// <summary>True if all bits are 0.</summary>
        public bool allFalse => data == 0u;
        /// <summary>True if all bits are 1.</summary>
        public bool allTrue => data == uint.MaxValue;
        string humanizedVersion => Convert.ToString(data, 2);
        /// <summary>Returns the bit array in a human readable form.</summary>
        public string humanizedData => System.Text.RegularExpressions.Regex.Replace(String.Format("{0, " + capacity + "}", Convert.ToString(data, 2)).Replace(' ', '0'), ".{8}", "$0.").TrimEnd('.');

        /// <summary>
        /// Returns the state of the bit at a specific index.
        /// </summary>
        /// <param name="index">Index of the bit.</param>
        /// <returns>State of the bit at the provided index.</returns>
        public bool this[uint index]
        {
            get => BitArrayUtilities.Get32(index, data);
            set => BitArrayUtilities.Set32(index, ref data, value);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="initValue">Initialization value.</param>
        public BitArray32(uint initValue) => data = initValue;
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bitIndexTrue">List of indices where bits should be set to true.</param>

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

        /// <summary>
        /// Bit-wise And
        /// </summary>
        /// <param name="other">Bit array with which to do the operation.</param>
        /// <returns>The resulting bit array.</returns>
        public IBitArray BitAnd(IBitArray other) => this & (BitArray32)other;
        /// <summary>
        /// Bit-wise Or
        /// </summary>
        /// <param name="other">Bit array with which to do the operation.</param>
        /// <returns>The resulting bit array.</returns>
        public IBitArray BitOr(IBitArray other) => this | (BitArray32)other;
        /// <summary>
        /// Bit-wise Not
        /// </summary>
        /// <returns>The resulting bit array.</returns>
        public IBitArray BitNot() => ~this;

        /// <summary>
        /// Bit-wise Not operator
        /// </summary>
        /// <param name="a">Bit array with which to do the operation.</param>
        /// <returns>The resulting bit array.</returns>
        public static BitArray32 operator ~(BitArray32 a) => new BitArray32(~a.data);
        /// <summary>
        /// Bit-wise Or operator
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>The resulting bit array.</returns>
        public static BitArray32 operator |(BitArray32 a, BitArray32 b) => new BitArray32(a.data | b.data);
        /// <summary>
        /// Bit-wise And operator
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>The resulting bit array.</returns>
        public static BitArray32 operator &(BitArray32 a, BitArray32 b) => new BitArray32(a.data & b.data);

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>True if both bit arrays are equals.</returns>
        public static bool operator ==(BitArray32 a, BitArray32 b) => a.data == b.data;
        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>True if the bit arrays are not equals.</returns>
        public static bool operator !=(BitArray32 a, BitArray32 b) => a.data != b.data;
        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="obj">Bit array to compare to.</param>
        /// <returns>True if the provided bit array is equal to this..</returns>
        public override bool Equals(object obj) => obj is BitArray32 ba32 && ba32.data == data;
        /// <summary>
        /// Get the hashcode of the bit array.
        /// </summary>
        /// <returns>Hashcode of the bit array.</returns>
        public override int GetHashCode() => 1768953197 + data.GetHashCode();
    }

    /// <summary>
    /// Bit array of size 64.
    /// </summary>
    [Serializable]
    [System.Diagnostics.DebuggerDisplay("{this.GetType().Name} {humanizedData}")]
    public struct BitArray64 : IBitArray
    {
        [SerializeField]
        ulong data;

        /// <summary>Number of elements in the bit array.</summary>
        public uint capacity => 64u;
        /// <summary>True if all bits are 0.</summary>
        public bool allFalse => data == 0uL;
        /// <summary>True if all bits are 1.</summary>
        public bool allTrue => data == ulong.MaxValue;
        /// <summary>Returns the bit array in a human readable form.</summary>
        public string humanizedData => System.Text.RegularExpressions.Regex.Replace(String.Format("{0, " + capacity + "}", Convert.ToString((long)data, 2)).Replace(' ', '0'), ".{8}", "$0.").TrimEnd('.');

        /// <summary>
        /// Returns the state of the bit at a specific index.
        /// </summary>
        /// <param name="index">Index of the bit.</param>
        /// <returns>State of the bit at the provided index.</returns>
        public bool this[uint index]
        {
            get => BitArrayUtilities.Get64(index, data);
            set => BitArrayUtilities.Set64(index, ref data, value);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="initValue">Initialization value.</param>
        public BitArray64(ulong initValue) => data = initValue;
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bitIndexTrue">List of indices where bits should be set to true.</param>

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

        /// <summary>
        /// Bit-wise Not operator
        /// </summary>
        /// <param name="a">Bit array with which to do the operation.</param>
        /// <returns>The resulting bit array.</returns>
        public static BitArray64 operator ~(BitArray64 a) => new BitArray64(~a.data);
        /// <summary>
        /// Bit-wise Or operator
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>The resulting bit array.</returns>
        public static BitArray64 operator |(BitArray64 a, BitArray64 b) => new BitArray64(a.data | b.data);
        /// <summary>
        /// Bit-wise And operator
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>The resulting bit array.</returns>
        public static BitArray64 operator &(BitArray64 a, BitArray64 b) => new BitArray64(a.data & b.data);

        /// <summary>
        /// Bit-wise And
        /// </summary>
        /// <param name="other">Bit array with which to do the operation.</param>
        /// <returns>The resulting bit array.</returns>
        public IBitArray BitAnd(IBitArray other) => this & (BitArray64)other;
        /// <summary>
        /// Bit-wise Or
        /// </summary>
        /// <param name="other">Bit array with which to do the operation.</param>
        /// <returns>The resulting bit array.</returns>
        public IBitArray BitOr(IBitArray other) => this | (BitArray64)other;
        /// <summary>
        /// Bit-wise Not
        /// </summary>
        /// <returns>The resulting bit array.</returns>
        public IBitArray BitNot() => ~this;

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>True if both bit arrays are equals.</returns>
        public static bool operator ==(BitArray64 a, BitArray64 b) => a.data == b.data;
        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>True if the bit arrays are not equals.</returns>
        public static bool operator !=(BitArray64 a, BitArray64 b) => a.data != b.data;
        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="obj">Bit array to compare to.</param>
        /// <returns>True if the provided bit array is equal to this..</returns>
        public override bool Equals(object obj) => obj is BitArray64 ba64 && ba64.data == data;
        /// <summary>
        /// Get the hashcode of the bit array.
        /// </summary>
        /// <returns>Hashcode of the bit array.</returns>
        public override int GetHashCode() => 1768953197 + data.GetHashCode();
    }

    /// <summary>
    /// Bit array of size 128.
    /// </summary>
    [Serializable]
    [System.Diagnostics.DebuggerDisplay("{this.GetType().Name} {humanizedData}")]
    public struct BitArray128 : IBitArray
    {
        [SerializeField]
        ulong data1;
        [SerializeField]
        ulong data2;

        /// <summary>Number of elements in the bit array.</summary>
        public uint capacity => 128u;
        /// <summary>True if all bits are 0.</summary>
        public bool allFalse => data1 == 0uL && data2 == 0uL;
        /// <summary>True if all bits are 1.</summary>
        public bool allTrue => data1 == ulong.MaxValue && data2 == ulong.MaxValue;
        /// <summary>Returns the bit array in a human readable form.</summary>
        public string humanizedData =>
            System.Text.RegularExpressions.Regex.Replace(String.Format("{0, " + 64u + "}", Convert.ToString((long)data2, 2)).Replace(' ', '0'), ".{8}", "$0.")
            + System.Text.RegularExpressions.Regex.Replace(String.Format("{0, " + 64u + "}", Convert.ToString((long)data1, 2)).Replace(' ', '0'), ".{8}", "$0.").TrimEnd('.');

        /// <summary>
        /// Returns the state of the bit at a specific index.
        /// </summary>
        /// <param name="index">Index of the bit.</param>
        /// <returns>State of the bit at the provided index.</returns>
        public bool this[uint index]
        {
            get => BitArrayUtilities.Get128(index, data1, data2);
            set => BitArrayUtilities.Set128(index, ref data1, ref data2, value);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="initValue1">Initialization value 1.</param>
        /// <param name="initValue2">Initialization value 2.</param>
        public BitArray128(ulong initValue1, ulong initValue2)
        {
            data1 = initValue1;
            data2 = initValue2;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bitIndexTrue">List of indices where bits should be set to true.</param>

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

        /// <summary>
        /// Bit-wise Not operator
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <returns>The resulting bit array.</returns>
        public static BitArray128 operator ~(BitArray128 a) => new BitArray128(~a.data1, ~a.data2);
        /// <summary>
        /// Bit-wise Or operator
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>The resulting bit array.</returns>
        public static BitArray128 operator |(BitArray128 a, BitArray128 b) => new BitArray128(a.data1 | b.data1, a.data2 | b.data2);
        /// <summary>
        /// Bit-wise And operator
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>The resulting bit array.</returns>
        public static BitArray128 operator &(BitArray128 a, BitArray128 b) => new BitArray128(a.data1 & b.data1, a.data2 & b.data2);

        /// <summary>
        /// Bit-wise And
        /// </summary>
        /// <param name="other">Bit array with which to do the operation.</param>
        /// <returns>The resulting bit array.</returns>
        public IBitArray BitAnd(IBitArray other) => this & (BitArray128)other;
        /// <summary>
        /// Bit-wise Or
        /// </summary>
        /// <param name="other">Bit array with which to do the operation.</param>
        /// <returns>The resulting bit array.</returns>
        public IBitArray BitOr(IBitArray other) => this | (BitArray128)other;
        /// <summary>
        /// Bit-wise Not
        /// </summary>
        /// <returns>The resulting bit array.</returns>
        public IBitArray BitNot() => ~this;

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>True if both bit arrays are equals.</returns>
        public static bool operator ==(BitArray128 a, BitArray128 b) => a.data1 == b.data1 && a.data2 == b.data2;
        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>True if the bit arrays are not equals.</returns>
        public static bool operator !=(BitArray128 a, BitArray128 b) => a.data1 != b.data1 || a.data2 != b.data2;
        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="obj">Bit array to compare to.</param>
        /// <returns>True if the provided bit array is equal to this..</returns>
        public override bool Equals(object obj) => obj is BitArray128 ba128 && data1.Equals(ba128.data1) && data2.Equals(ba128.data2);
        /// <summary>
        /// Get the hashcode of the bit array.
        /// </summary>
        /// <returns>Hashcode of the bit array.</returns>
        public override int GetHashCode()
        {
            var hashCode = 1755735569;
            hashCode = hashCode * -1521134295 + data1.GetHashCode();
            hashCode = hashCode * -1521134295 + data2.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// Bit array of size 256.
    /// </summary>
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

        /// <summary>Number of elements in the bit array.</summary>
        public uint capacity => 256u;
        /// <summary>True if all bits are 0.</summary>
        public bool allFalse => data1 == 0uL && data2 == 0uL && data3 == 0uL && data4 == 0uL;
        /// <summary>True if all bits are 1.</summary>
        public bool allTrue => data1 == ulong.MaxValue && data2 == ulong.MaxValue && data3 == ulong.MaxValue && data4 == ulong.MaxValue;
        /// <summary>Returns the bit array in a human readable form.</summary>
        public string humanizedData =>
            System.Text.RegularExpressions.Regex.Replace(String.Format("{0, " + 64u + "}", Convert.ToString((long)data4, 2)).Replace(' ', '0'), ".{8}", "$0.")
            + System.Text.RegularExpressions.Regex.Replace(String.Format("{0, " + 64u + "}", Convert.ToString((long)data3, 2)).Replace(' ', '0'), ".{8}", "$0.")
            + System.Text.RegularExpressions.Regex.Replace(String.Format("{0, " + 64u + "}", Convert.ToString((long)data2, 2)).Replace(' ', '0'), ".{8}", "$0.")
            + System.Text.RegularExpressions.Regex.Replace(String.Format("{0, " + 64u + "}", Convert.ToString((long)data1, 2)).Replace(' ', '0'), ".{8}", "$0.").TrimEnd('.');

        /// <summary>
        /// Returns the state of the bit at a specific index.
        /// </summary>
        /// <param name="index">Index of the bit.</param>
        /// <returns>State of the bit at the provided index.</returns>
        public bool this[uint index]
        {
            get => BitArrayUtilities.Get256(index, data1, data2, data3, data4);
            set => BitArrayUtilities.Set256(index, ref data1, ref data2, ref data3, ref data4, value);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="initValue1">Initialization value 1.</param>
        /// <param name="initValue2">Initialization value 2.</param>
        /// <param name="initValue3">Initialization value 3.</param>
        /// <param name="initValue4">Initialization value 4.</param>
        public BitArray256(ulong initValue1, ulong initValue2, ulong initValue3, ulong initValue4)
        {
            data1 = initValue1;
            data2 = initValue2;
            data3 = initValue3;
            data4 = initValue4;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bitIndexTrue">List of indices where bits should be set to true.</param>
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

        /// <summary>
        /// Bit-wise Not operator
        /// </summary>
        /// <param name="a">Bit array with which to do the operation.</param>
        /// <returns>The resulting bit array.</returns>
        public static BitArray256 operator ~(BitArray256 a) => new BitArray256(~a.data1, ~a.data2, ~a.data3, ~a.data4);
        /// <summary>
        /// Bit-wise Or operator
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>The resulting bit array.</returns>
        public static BitArray256 operator |(BitArray256 a, BitArray256 b) => new BitArray256(a.data1 | b.data1, a.data2 | b.data2, a.data3 | b.data3, a.data4 | b.data4);
        /// <summary>
        /// Bit-wise And operator
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>The resulting bit array.</returns>
        public static BitArray256 operator &(BitArray256 a, BitArray256 b) => new BitArray256(a.data1 & b.data1, a.data2 & b.data2, a.data3 & b.data3, a.data4 & b.data4);

        /// <summary>
        /// Bit-wise And
        /// </summary>
        /// <param name="other">Bit array with which to do the operation.</param>
        /// <returns>The resulting bit array.</returns>
        public IBitArray BitAnd(IBitArray other) => this & (BitArray256)other;
        /// <summary>
        /// Bit-wise Or
        /// </summary>
        /// <param name="other">Bit array with which to do the operation.</param>
        /// <returns>The resulting bit array.</returns>
        public IBitArray BitOr(IBitArray other) => this | (BitArray256)other;
        /// <summary>
        /// Bit-wise Not
        /// </summary>
        /// <returns>The resulting bit array.</returns>
        public IBitArray BitNot() => ~this;

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>True if both bit arrays are equals.</returns>
        public static bool operator ==(BitArray256 a, BitArray256 b) => a.data1 == b.data1 && a.data2 == b.data2 && a.data3 == b.data3 && a.data4 == b.data4;
        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="a">First bit array.</param>
        /// <param name="b">Second bit array.</param>
        /// <returns>True if the bit arrays are not equals.</returns>
        public static bool operator !=(BitArray256 a, BitArray256 b) => a.data1 != b.data1 || a.data2 != b.data2 || a.data3 != b.data3 || a.data4 != b.data4;
        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="obj">Bit array to compare to.</param>
        /// <returns>True if the provided bit array is equal to this..</returns>
        public override bool Equals(object obj)
            => obj is BitArray256 ba256
            && data1.Equals(ba256.data1)
            && data2.Equals(ba256.data2)
            && data3.Equals(ba256.data3)
            && data4.Equals(ba256.data4);
        /// <summary>
        /// Get the hashcode of the bit array.
        /// </summary>
        /// <returns>Hashcode of the bit array.</returns>
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

    /// <summary>
    /// Bit array utility class.
    /// </summary>
    public static class BitArrayUtilities
    {
        //written here to not duplicate the serialized accessor and runtime accessor

        /// <summary>
        /// Get a bit at a specific index.
        /// </summary>
        /// <param name="index">Bit index.</param>
        /// <param name="data">Bit array data.</param>
        /// <returns>The value of the bit at the specific index.</returns>
        public static bool Get8(uint index, byte data) => (data & (1u << (int)index)) != 0u;
        /// <summary>
        /// Get a bit at a specific index.
        /// </summary>
        /// <param name="index">Bit index.</param>
        /// <param name="data">Bit array data.</param>
        /// <returns>The value of the bit at the specific index.</returns>
        public static bool Get16(uint index, ushort data) => (data & (1u << (int)index)) != 0u;
        /// <summary>
        /// Get a bit at a specific index.
        /// </summary>
        /// <param name="index">Bit index.</param>
        /// <param name="data">Bit array data.</param>
        /// <returns>The value of the bit at the specific index.</returns>
        public static bool Get32(uint index, uint data) => (data & (1u << (int)index)) != 0u;
        /// <summary>
        /// Get a bit at a specific index.
        /// </summary>
        /// <param name="index">Bit index.</param>
        /// <param name="data">Bit array data.</param>
        /// <returns>The value of the bit at the specific index.</returns>
        public static bool Get64(uint index, ulong data) => (data & (1uL << (int)index)) != 0uL;
        /// <summary>
        /// Get a bit at a specific index.
        /// </summary>
        /// <param name="index">Bit index.</param>
        /// <param name="data1">Bit array data 1.</param>
        /// <param name="data2">Bit array data 2.</param>
        /// <returns>The value of the bit at the specific index.</returns>
        public static bool Get128(uint index, ulong data1, ulong data2)
            => index < 64u
            ? (data1 & (1uL << (int)index)) != 0uL
            : (data2 & (1uL << (int)(index - 64u))) != 0uL;
        /// <summary>
        /// Get a bit at a specific index.
        /// </summary>
        /// <param name="index">Bit index.</param>
        /// <param name="data1">Bit array data 1.</param>
        /// <param name="data2">Bit array data 2.</param>
        /// <param name="data3">Bit array data 3.</param>
        /// <param name="data4">Bit array data 4.</param>
        /// <returns>The value of the bit at the specific index.</returns>
        public static bool Get256(uint index, ulong data1, ulong data2, ulong data3, ulong data4)
            => index < 128u
            ? index < 64u
            ? (data1 & (1uL << (int)index)) != 0uL
            : (data2 & (1uL << (int)(index - 64u))) != 0uL
            : index < 192u
            ? (data3 & (1uL << (int)(index - 128u))) != 0uL
            : (data4 & (1uL << (int)(index - 192u))) != 0uL;

        /// <summary>
        /// Set a bit at a specific index.
        /// </summary>
        /// <param name="index">Bit index.</param>
        /// <param name="data">Bit array data.</param>
        /// <param name="value">Value to set the bit to.</param>
        public static void Set8(uint index, ref byte data, bool value) => data = (byte)(value ? (data | (1u << (int)index)) : (data & ~(1u << (int)index)));
        /// <summary>
        /// Set a bit at a specific index.
        /// </summary>
        /// <param name="index">Bit index.</param>
        /// <param name="data">Bit array data.</param>
        /// <param name="value">Value to set the bit to.</param>
        public static void Set16(uint index, ref ushort data, bool value) => data = (ushort)(value ? (data | (1u << (int)index)) : (data & ~(1u << (int)index)));
        /// <summary>
        /// Set a bit at a specific index.
        /// </summary>
        /// <param name="index">Bit index.</param>
        /// <param name="data">Bit array data.</param>
        /// <param name="value">Value to set the bit to.</param>
        public static void Set32(uint index, ref uint data, bool value) => data = (value ? (data | (1u << (int)index)) : (data & ~(1u << (int)index)));
        /// <summary>
        /// Set a bit at a specific index.
        /// </summary>
        /// <param name="index">Bit index.</param>
        /// <param name="data">Bit array data.</param>
        /// <param name="value">Value to set the bit to.</param>
        public static void Set64(uint index, ref ulong data, bool value) => data = (value ? (data | (1uL << (int)index)) : (data & ~(1uL << (int)index)));
        /// <summary>
        /// Set a bit at a specific index.
        /// </summary>
        /// <param name="index">Bit index.</param>
        /// <param name="data1">Bit array data 1.</param>
        /// <param name="data2">Bit array data 2.</param>
        /// <param name="value">Value to set the bit to.</param>
        public static void Set128(uint index, ref ulong data1, ref ulong data2, bool value)
        {
            if (index < 64u)
                data1 = (value ? (data1 | (1uL << (int)index)) : (data1 & ~(1uL << (int)index)));
            else
                data2 = (value ? (data2 | (1uL << (int)(index - 64u))) : (data2 & ~(1uL << (int)(index - 64u))));
        }

        /// <summary>
        /// Set a bit at a specific index.
        /// </summary>
        /// <param name="index">Bit index.</param>
        /// <param name="data1">Bit array data 1.</param>
        /// <param name="data2">Bit array data 2.</param>
        /// <param name="data3">Bit array data 3.</param>
        /// <param name="data4">Bit array data 4.</param>
        /// <param name="value">Value to set the bit to.</param>
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
