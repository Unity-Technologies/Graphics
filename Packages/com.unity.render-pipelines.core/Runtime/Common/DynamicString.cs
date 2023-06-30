using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A mutable string with a size and capacity so you can do string manipulations wile avoiding GC allocs.
    /// </summary>
    /// <typeparam name="T">Type of the array.</typeparam>
    [DebuggerDisplay("Size = {size} Capacity = {capacity}")]
    public class DynamicString : DynamicArray<char>
    {
        /// <summary>
        /// Create a DynamicString string with the default capacity.
        /// </summary>
        public DynamicString() : base()
        {}

        /// <summary>
        /// Create a DynamicString given a string.
        /// </summary>
        /// <param name="s">The string to initialize with.</param>
        public DynamicString(string s) : base(s.Length, true)
        {
            for (int i = 0; i < s.Length; ++i)
                m_Array[i] = s[i];
        }

        /// <summary>
        /// Allocate an empty dynamic string with the given number of characters allocated.
        /// </summary>
        /// <param name="capacity">The number of characters to pre-allocate.</param>
        public DynamicString(int capacity) : base(capacity, false) { }

        /// <summary>
        /// Append a string to the DynamicString. This will not allocate memory if the capacity is still sufficient.
        /// </summary>
        /// <param name="s"></param>
        public void Append(string s)
        {
            int offset = size;
            Reserve(size + s.Length, true);
            for (int i = 0; i < s.Length; ++i)
                m_Array[offset+i] = s[i];
            size += s.Length;
            BumpVersion();
        }

        public void Append(DynamicString s) => AddRange(s);

        public override string ToString()
        {
            return new string(m_Array, 0, size);
        }
    }

}
