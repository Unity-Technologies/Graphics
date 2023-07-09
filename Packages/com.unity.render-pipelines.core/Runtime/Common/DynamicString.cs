using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A mutable string with a size and capacity so you can do string manipulations wile avoiding GC allocs.
    /// </summary>
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
        /// <param name="s">The string to append.</param>
        public void Append(string s)
        {
            int offset = size;
            Reserve(size + s.Length, true);
            for (int i = 0; i < s.Length; ++i)
                m_Array[offset+i] = s[i];
            size += s.Length;
            BumpVersion();
        }

        /// <summary>
        /// Append a DynamicString to this DynamicString.
        /// </summary>
        /// <param name="s">The string to append.</param>
        public void Append(DynamicString s) => AddRange(s);

        /// <summary>
        /// Convert the DyanamicString back to a regular c# string.
        /// </summary>
        /// <returns>A new string with the same contents at the dynamic string.</returns>
        public override string ToString()
        {
            return new string(m_Array, 0, size);
        }
    }

}
