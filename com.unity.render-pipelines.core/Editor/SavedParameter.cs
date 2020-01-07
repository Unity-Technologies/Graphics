using System;
using UnityEngine.Assertions;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// An utility class to save and load data to a custom location, usually in editor preferences.
    /// </summary>
    /// <typeparam name="T">The type of data to save</typeparam>
    /// <seealso cref="SavedBool"/>
    /// <seealso cref="SavedInt"/>
    /// <seealso cref="SavedFloat"/>
    /// <seealso cref="SavedString"/>
    public class SavedParameter<T>
        where T : IEquatable<T>
    {
        /// <summary>
        /// The method called when the data is being stored.
        /// </summary>
        /// <param name="key">The string key to associate the data with.</param>
        /// <param name="value">The value to store.</param>
        public delegate void SetParameter(string key, T value);

        /// <summary>
        /// The method called when the data is being retrieved.
        /// </summary>
        /// <param name="key">The string key associated with the data to retrieve.</param>
        /// <param name="defaultValue">A default value to return if the key doesn't exist.</param>
        /// <returns>The value associated to the key, <paramref name="defaultValue"/> otherwise.</returns>
        public delegate T GetParameter(string key, T defaultValue);

        readonly string m_Key;
        bool m_Loaded;
        T m_Value;

        readonly SetParameter m_Setter;
        readonly GetParameter m_Getter;

        /// <summary>
        /// Creates a new instance of a <see cref="SavedParameter{T}"/>.
        /// </summary>
        /// <param name="key">The string key to associate the data with.</param>
        /// <param name="value">The initial value to set the parameter to if the key doesn't exist yet.</param>
        /// <param name="getter">The method called when the data is being retrieved.</param>
        /// <param name="setter">The method called when the data is being stored.</param>
        public SavedParameter(string key, T value, GetParameter getter, SetParameter setter)
        {
            Assert.IsNotNull(setter);
            Assert.IsNotNull(getter);

            m_Key = key;
            m_Loaded = false;
            m_Value = value;
            m_Setter = setter;
            m_Getter = getter;
        }

        void Load()
        {
            if (m_Loaded)
                return;

            m_Loaded = true;
            m_Value = m_Getter(m_Key, m_Value);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public T value
        {
            get
            {
                Load();
                return m_Value;
            }
            set
            {
                Load();

                if (m_Value.Equals(value))
                    return;

                m_Value = value;
                m_Setter(m_Key, value);
            }
        }
    }

    // Pre-specialized class for easier use and compatibility with existing code

    /// <summary>
    /// A boolean value stored in editor preferences.
    /// </summary>
    public sealed class SavedBool : SavedParameter<bool>
    {
        /// <summary>
        /// Creates a new instance of a <see cref="SavedBool"/>.
        /// </summary>
        /// <param name="key">The string key to associate the data with.</param>
        /// <param name="value">The initial value to set the parameter to if the key doesn't exist yet.</param>
        public SavedBool(string key, bool value)
            : base(key, value, EditorPrefs.GetBool, EditorPrefs.SetBool) { }
    }

    /// <summary>
    /// An integer value stored in editor preferences.
    /// </summary>
    public sealed class SavedInt : SavedParameter<int>
    {
        /// <summary>
        /// Creates a new instance of a <see cref="SavedInt"/>.
        /// </summary>
        /// <param name="key">The string key to associate the data with.</param>
        /// <param name="value">The initial value to set the parameter to if the key doesn't exist yet.</param>
        public SavedInt(string key, int value)
            : base(key, value, EditorPrefs.GetInt, EditorPrefs.SetInt) { }
    }

    /// <summary>
    /// A floating point value stored in editor preferences.
    /// </summary>
    public sealed class SavedFloat : SavedParameter<float>
    {
        /// <summary>
        /// Creates a new instance of a <see cref="SavedFloat"/>.
        /// </summary>
        /// <param name="key">The string key to associate the data with.</param>
        /// <param name="value">The initial value to set the parameter to if the key doesn't exist yet.</param>
        public SavedFloat(string key, float value)
            : base(key, value, EditorPrefs.GetFloat, EditorPrefs.SetFloat) { }
    }

    /// <summary>
    /// A string value stored in editor preferences.
    /// </summary>
    public sealed class SavedString : SavedParameter<string>
    {
        /// <summary>
        /// Creates a new instance of a <see cref="SavedString"/>.
        /// </summary>
        /// <param name="key">The string key to associate the data with.</param>
        /// <param name="value">The initial value to set the parameter to if the key doesn't exist yet.</param>
        public SavedString(string key, string value)
            : base(key, value, EditorPrefs.GetString, EditorPrefs.SetString) { }
    }
}
