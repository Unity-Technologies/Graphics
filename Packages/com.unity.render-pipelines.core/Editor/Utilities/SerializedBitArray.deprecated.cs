using System;

namespace UnityEditor.Rendering
{
    /// <summary>Serialisation of BitArray, Utility class</summary>
    public static partial class SerializedBitArrayUtilities
    {
        /// <summary>Convert to 8bit</summary>
        /// <param name="serializedProperty">The SerializedProperty</param>
        /// <returns>A SerializedBitArray8</returns>
        [Obsolete("Since SerializedBitArray unification, only use SerializedBitArrayAny. #from(23.2)")]
        public static SerializedBitArray8 ToSerializeBitArray8(this SerializedProperty serializedProperty) => null;

        /// <summary>Try convert to 8bit</summary>
        /// <param name="serializedProperty">The SerializedProperty</param>
        /// <param name="serializedBitArray">Out SerializedBitArray8</param>
        /// <returns>True if convertion was a success</returns>
        [Obsolete("Since SerializedBitArray unification, only use SerializedBitArrayAny. #from(23.2)")]
        public static bool TryGetSerializeBitArray8(this SerializedProperty serializedProperty, out SerializedBitArray8 serializedBitArray)
        {
            serializedBitArray = null;
            return false;
        }

        /// <summary>Convert to 16bit</summary>
        /// <param name="serializedProperty">The SerializedProperty</param>
        /// <returns>A SerializedBitArray16</returns>
        [Obsolete("Since SerializedBitArray unification, only use SerializedBitArrayAny. #from(23.2)")]
        public static SerializedBitArray16 ToSerializeBitArray16(this SerializedProperty serializedProperty) => null;

        /// <summary>Try convert to 16bit</summary>
        /// <param name="serializedProperty">The SerializedProperty</param>
        /// <param name="serializedBitArray">Out SerializedBitArray16</param>
        /// <returns>True if convertion was a success</returns>
        [Obsolete("Since SerializedBitArray unification, only use SerializedBitArrayAny. #from(23.2)")]
        public static bool TryGetSerializeBitArray16(this SerializedProperty serializedProperty, out SerializedBitArray16 serializedBitArray)
        {
            serializedBitArray = null;
            return false;
        }

        /// <summary>Convert to 32bit</summary>
        /// <param name="serializedProperty">The SerializedProperty</param>
        /// <returns>A SerializedBitArray32</returns>
        [Obsolete("Since SerializedBitArray unification, only use SerializedBitArrayAny. #from(23.2)")]
        public static SerializedBitArray32 ToSerializeBitArray32(this SerializedProperty serializedProperty) => null;

        /// <summary>Try convert to 32bit</summary>
        /// <param name="serializedProperty">The SerializedProperty</param>
        /// <param name="serializedBitArray">Out SerializedBitArray32</param>
        /// <returns>True if convertion was a success</returns>
        [Obsolete("Since SerializedBitArray unification, only use SerializedBitArrayAny. #from(23.2)")]
        public static bool TryGetSerializeBitArray32(this SerializedProperty serializedProperty, out SerializedBitArray32 serializedBitArray)
        {
            serializedBitArray = null;
            return false;
        }

        /// <summary>Convert to 64bit</summary>
        /// <param name="serializedProperty">The SerializedProperty</param>
        /// <returns>A SerializedBitArray64</returns>
        [Obsolete("Since SerializedBitArray unification, only use SerializedBitArrayAny. #from(23.2)")]
        public static SerializedBitArray64 ToSerializeBitArray64(this SerializedProperty serializedProperty) => null;

        /// <summary>Try convert to 64bit</summary>
        /// <param name="serializedProperty">The SerializedProperty</param>
        /// <param name="serializedBitArray">Out SerializedBitArray64</param>
        /// <returns>True if convertion was a success</returns>
        [Obsolete("Since SerializedBitArray unification, only use SerializedBitArrayAny. #from(23.2)")]
        public static bool TryGetSerializeBitArray64(this SerializedProperty serializedProperty, out SerializedBitArray64 serializedBitArray)
        {
            serializedBitArray = null;
            return false;
        }

        /// <summary>Convert to 128bit</summary>
        /// <param name="serializedProperty">The SerializedProperty</param>
        /// <returns>A SerializedBitArray128</returns>
        [Obsolete("Since SerializedBitArray unification, only use SerializedBitArrayAny. #from(23.2)")]
        public static SerializedBitArray128 ToSerializeBitArray128(this SerializedProperty serializedProperty) => null;

        /// <summary>Try convert to 128bit</summary>
        /// <param name="serializedProperty">The SerializedProperty</param>
        /// <param name="serializedBitArray">Out SerializedBitArray128</param>
        /// <returns>True if convertion was a success</returns>
        [Obsolete("Since SerializedBitArray unification, only use SerializedBitArrayAny. #from(23.2)")]
        public static bool TryGetSerializeBitArray128(this SerializedProperty serializedProperty, out SerializedBitArray128 serializedBitArray)
        {
            serializedBitArray = null;
            return false;
        }

        /// <summary>Convert to 256bit</summary>
        /// <param name="serializedProperty">The SerializedProperty</param>
        /// <returns>A SerializedBitArray256</returns>
        [Obsolete("Since SerializedBitArray unification, only use SerializedBitArrayAny. #from(23.2)")]
        public static SerializedBitArray256 ToSerializeBitArray256(this SerializedProperty serializedProperty) => null;

        /// <summary>Try convert to 256bit</summary>
        /// <param name="serializedProperty">The SerializedProperty</param>
        /// <param name="serializedBitArray">Out SerializedBitArray256</param>
        /// <returns>True if convertion was a success</returns>
        [Obsolete("Since SerializedBitArray unification, only use SerializedBitArrayAny. #from(23.2)")]
        public static bool TryGetSerializeBitArray256(this SerializedProperty serializedProperty, out SerializedBitArray256 serializedBitArray)
        {
            serializedBitArray = null;
            return false;
        }
    }

    /// <summary>Abstract base classe of all SerializedBitArray</summary>
    [Obsolete("Since SerializedBitArray unification, only use SerializedBitArrayAny. #from(23.2)")]
    public abstract class SerializedBitArray : ISerializedBitArray
    {
        /// <summary>Capacity of the bitarray</summary>
        public uint capacity { get; }

        internal SerializedBitArray(SerializedProperty serializedProperty, uint capacity)
        {}

        /// <summary>Does the bit at given index have multiple different values?</summary>
        /// <param name="bitIndex">The index</param>
        /// <returns>True: Multiple different value for the given bit index</returns>
        public bool HasBitMultipleDifferentValue(uint bitIndex) => default;

        /// <summary>Get the bit at given index</summary>
        /// <param name="bitIndex">The index</param>
        /// <returns>Bit value</returns>
        public bool GetBitAt(uint bitIndex) => default;

        /// <summary>Set the bit at given index</summary>
        /// <param name="bitIndex">The index</param>
        /// <param name="value">The value</param>
        public void SetBitAt(uint bitIndex, bool value) { }

        /// <summary>Sync again every serializedProperty</summary>
        protected void ResyncSerialization() { }

        /// <summary>Sync the reflected value with target value change</summary>
        public void Update() { }

        /// <summary>Set the bit at given index</summary>
        [Obsolete("Was only working under specific 32 bit mask size. Removed for disambiguisation. Use SetBitAt instead. #from(23.2)")]
        protected static Action<SerializedProperty, int, bool> SetBitAtIndexForAllTargetsImmediate = (SerializedProperty sp, int index, bool value) => { };
        // Note: this should be exposed at the same time as issue with type other than Int32 is fixed on C++ side
        /// <summary>Has multiple differente value bitwise</summary>
        [Obsolete("Was only working under specific 32 bit mask size. Removed for disambiguisation. Use HasBitMultipleDifferentValueBitwise_For64BitsOrLess instead. #from(23.2)")]
        protected static Func<SerializedProperty, int> HasMultipleDifferentValuesBitwise = (SerializedProperty sp) => 0;

        /// <summary>The underlying serialized property</summary>
        [Obsolete("As it is required to discompose per target to isolate works per bit, this cannot be used when there is multiple selection. #from(23.2)")]
        protected SerializedProperty m_SerializedProperty;

        /// <summary>Initialisation of dedicated SerializedPropertiws</summary>
        /// <returns>Arrays of SerializedProperty</returns>
        [Obsolete("Use m_SerializedProperty only to prevent desynchronisation between objects #from(23.2)")]
        protected SerializedProperty[] GetOrInitializeSerializedProperties()
            => new SerializedProperty[] { };

        /// <summary>Say if the properties have differente values</summary>
        /// <param name="bitIndex">The index</param>
        /// <returns>True: properties have different value</returns>
        [Obsolete("Replaced by an autocasting to 64 bit buckets for all IBitArray. Now difference computation is not implementation specific anymore #from(23.2)")]
        virtual protected bool HasBitMultipleDifferentValue_Internal(uint bitIndex) => HasBitMultipleDifferentValue(bitIndex);

        /// <summary>
        /// Safety: serializedProperty must match its path
        /// </summary>
        /// <param name="propertyPath">serializedProperty must match its path</param>
        /// <param name="serializedProperty">serializedProperty must match its path</param>
        /// <param name="bitIndex"></param>
        /// <returns></returns>
        [Obsolete("Replaced by HasBitMultipleDifferentValue that now works for all IBitArray implementations. #from(23.2)")]
        unsafe protected bool HasBitMultipleDifferentValue_For64Bits(string propertyPath, SerializedProperty serializedProperty, uint bitIndex)
            => HasBitMultipleDifferentValue(bitIndex);

        /// <summary>Get the value at index</summary>
        /// <param name="bitIndex">The index</param>
        /// <returns>Value at the index</returns>
        [Obsolete("Replaced by GetBitAt that now works for all IBitArray implementations. #from(23.2)")]
        virtual protected bool GetBitAt_Internal(uint bitIndex) => GetBitAt(bitIndex);

        /// <summary>Set the bit at given index</summary>
        /// <param name="bitIndex">The index</param>
        /// <param name="value">The value</param>
        [Obsolete("Replaced by SetBitAt that now works for all IBitArray implementations. #from(23.2)")]
        virtual protected void SetBitAt_Internal(uint bitIndex, bool value) => SetBitAt(bitIndex, value);

        /// <summary>
        /// Update all the targets at specific bit index only
        /// </summary>
        /// <param name="serializedProperty">The serializedProperty to update</param>
        /// <param name="bitIndex">Index to assign the value</param>
        /// <param name="value">Boolean value that the bit should be updated to</param>
        [Obsolete("Replaced by SetBitAt that now works for all IBitArray implementations. #from(23.2)")]
        protected void SetBitAt_For64Bits(SerializedProperty serializedProperty, uint bitIndex, bool value)
            => SetBitAtIndexForAllTargetsImmediate(serializedProperty, (int)bitIndex, value);
    }

    /// <summary>SerializedBitArray spetialized for 8bit capacity</summary>
    [Obsolete("Since SerializedBitArray unification, only use SerializedBitArrayAny. #from(23.2)")]
    public sealed class SerializedBitArray8 : SerializedBitArray
    {
        /// <inheritdoc/>
        public SerializedBitArray8(SerializedProperty serializedProperty, uint capacity) : base(serializedProperty, capacity) { }

        /// <inheritdoc/>
        [Obsolete]
        protected override bool GetBitAt_Internal(uint bitIndex) => default;

        /// <inheritdoc/>
        [Obsolete]
        protected override bool HasBitMultipleDifferentValue_Internal(uint bitIndex) => default;

        /// <inheritdoc/>
        [Obsolete]
        protected override void SetBitAt_Internal(uint bitIndex, bool value) { }
    }

    /// <summary>SerializedBitArray spetialized for 8bit capacity</summary>
    [Obsolete("Since SerializedBitArray unification, only use SerializedBitArrayAny. #from(23.2)")]
    public sealed class SerializedBitArray16 : SerializedBitArray
    {
        /// <inheritdoc/>
        public SerializedBitArray16(SerializedProperty serializedProperty, uint capacity) : base(serializedProperty, capacity) { }

        /// <inheritdoc/>
        [Obsolete]
        protected override bool GetBitAt_Internal(uint bitIndex) => default;

        /// <inheritdoc/>
        [Obsolete]
        protected override bool HasBitMultipleDifferentValue_Internal(uint bitIndex) => default;

        /// <inheritdoc/>
        [Obsolete]
        protected override void SetBitAt_Internal(uint bitIndex, bool value) { }
    }

    /// <summary>SerializedBitArray spetialized for 8bit capacity</summary>
    [Obsolete("Since SerializedBitArray unification, only use SerializedBitArrayAny. #from(23.2)")]
    public sealed class SerializedBitArray32 : SerializedBitArray
    {
        /// <inheritdoc/>
        public SerializedBitArray32(SerializedProperty serializedProperty, uint capacity) : base(serializedProperty, capacity) { }

        /// <inheritdoc/>
        [Obsolete]
        protected override bool GetBitAt_Internal(uint bitIndex) => default;

        /// <inheritdoc/>
        [Obsolete]
        protected override bool HasBitMultipleDifferentValue_Internal(uint bitIndex) => default;

        /// <inheritdoc/>
        [Obsolete]
        protected override void SetBitAt_Internal(uint bitIndex, bool value) { }
    }

    /// <summary>SerializedBitArray spetialized for 8bit capacity</summary>
    [Obsolete("Since SerializedBitArray unification, only use SerializedBitArrayAny. #from(23.2)")]
    public sealed class SerializedBitArray64 : SerializedBitArray
    {
        /// <inheritdoc/>
        public SerializedBitArray64(SerializedProperty serializedProperty, uint capacity) : base(serializedProperty, capacity) { }

        /// <inheritdoc/>
        [Obsolete]
        protected override bool GetBitAt_Internal(uint bitIndex) => default;

        /// <inheritdoc/>
        [Obsolete]
        protected override bool HasBitMultipleDifferentValue_Internal(uint bitIndex) => default;

        /// <inheritdoc/>
        [Obsolete]
        protected override void SetBitAt_Internal(uint bitIndex, bool value) { }
    }

    /// <summary>SerializedBitArray spetialized for 8bit capacity</summary>
    [Obsolete("Since SerializedBitArray unification, only use SerializedBitArrayAny. #from(23.2)")]
    public sealed class SerializedBitArray128 : SerializedBitArray
    {
        /// <inheritdoc/>
        public SerializedBitArray128(SerializedProperty serializedProperty, uint capacity) : base(serializedProperty, capacity) { }

        /// <inheritdoc/>
        [Obsolete]
        protected override bool GetBitAt_Internal(uint bitIndex) => default;

        /// <inheritdoc/>
        [Obsolete]
        protected override bool HasBitMultipleDifferentValue_Internal(uint bitIndex) => default;

        /// <inheritdoc/>
        [Obsolete]
        protected override void SetBitAt_Internal(uint bitIndex, bool value) { }
    }

    /// <summary>SerializedBitArray spetialized for 8bit capacity</summary>
    [Obsolete("Since SerializedBitArray unification, only use SerializedBitArrayAny. #from(23.2)")]
    public sealed class SerializedBitArray256 : SerializedBitArray
    {
        /// <inheritdoc/>
        public SerializedBitArray256(SerializedProperty serializedProperty, uint capacity) : base(serializedProperty, capacity) { }

        /// <inheritdoc/>
        [Obsolete]
        protected override bool GetBitAt_Internal(uint bitIndex) => default;

        /// <inheritdoc/>
        [Obsolete]
        protected override bool HasBitMultipleDifferentValue_Internal(uint bitIndex) => default;

        /// <inheritdoc/>
        [Obsolete]
        protected override void SetBitAt_Internal(uint bitIndex, bool value) { }
    }
}
