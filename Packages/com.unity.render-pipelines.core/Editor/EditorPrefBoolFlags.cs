using System;

namespace UnityEditor.Rendering
{
    /// <summary>Bool flag saved in EditorPref</summary>
    /// <typeparam name="T">Underlying enum type</typeparam>
    public struct EditorPrefBoolFlags<T> : IEquatable<T>, IEquatable<EditorPrefBoolFlags<T>>
        where T : struct, IConvertible
    {
        readonly string m_Key;

        /// <summary>The value as the underlying enum type used</summary>
        public T value
        { get => (T)(object)EditorPrefs.GetInt(m_Key); set => EditorPrefs.SetInt(m_Key, (int)(object)value); }

        /// <summary>The raw value</summary>
        public uint rawValue
        {
            get => (uint)EditorPrefs.GetInt(m_Key);
            set => EditorPrefs.SetInt(m_Key, (int)value);
        }

        /// <summary>Constructor</summary>
        /// <param name="key">Name of the Key in EditorPrefs to save the value</param>
        public EditorPrefBoolFlags(string key)
        {
            m_Key = key;
        }

        /// <summary>Test if saved value is equal to the one given</summary>
        /// <param name="other">Given value</param>
        /// <returns>True if value are the same</returns>
        public bool Equals(T other) => (int)(object)value == (int)(object)other;

        /// <summary>Test if this EditorPrefBoolFlags is the same than the given one</summary>
        /// <param name="other">Given EditorPrefBoolFlags</param>
        /// <returns>True if they use the same value</returns>
        public bool Equals(EditorPrefBoolFlags<T> other) => m_Key == other.m_Key;

        /// <summary>Test if the given flags are set</summary>
        /// <param name="v">Given flags</param>
        /// <returns>True: all the given flags are set</returns>
        public bool HasFlag(T v) => ((uint)(int)(object)v & rawValue) == (uint)(int)(object)v;
        /// <summary>Set or unset the flags</summary>
        /// <param name="f">Flags to edit</param>
        /// <param name="v">Boolean value to set to the given flags</param>
        public void SetFlag(T f, bool v)
        {
            if (v) rawValue |= (uint)(int)(object)f;
            else rawValue &= ~(uint)(int)(object)f;
        }

        /// <summary>Explicit conversion operator to the underlying type</summary>
        /// <param name="v">The EditorPrefBoolFlags to convert</param>
        /// <returns>The converted value</returns>
        public static explicit operator T(EditorPrefBoolFlags<T> v) => v.value;
        /// <summary>Or operator between a EditorPrefBoolFlags and a value</summary>
        /// <param name="l">The EditorPrefBoolFlags</param>
        /// <param name="r">The value</param>
        /// <returns>A EditorPrefBoolFlags with OR operator performed</returns>
        public static EditorPrefBoolFlags<T> operator |(EditorPrefBoolFlags<T> l, T r)
        {
            l.rawValue |= (uint)(int)(object)r;
            return l;
        }

        /// <summary>And operator between a EditorPrefBoolFlags and a value</summary>
        /// <param name="l">The EditorPrefBoolFlags</param>
        /// <param name="r">The value</param>
        /// <returns>A EditorPrefBoolFlags with AND operator performed</returns>
        public static EditorPrefBoolFlags<T> operator &(EditorPrefBoolFlags<T> l, T r)
        {
            l.rawValue &= (uint)(int)(object)r;
            return l;
        }

        /// <summary>Xor operator between a EditorPrefBoolFlags and a value</summary>
        /// <param name="l">The EditorPrefBoolFlags</param>
        /// <param name="r">The value</param>
        /// <returns>A EditorPrefBoolFlags with XOR operator performed</returns>
        public static EditorPrefBoolFlags<T> operator ^(EditorPrefBoolFlags<T> l, T r)
        {
            l.rawValue ^= (uint)(int)(object)r;
            return l;
        }
    }
}
