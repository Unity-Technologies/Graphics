using System;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Defines an attribute that constitutes the payload of a system.
    /// </summary>
    [Serializable]
    /*public*/ class Attribute
    {
        [SerializeField]
        private string m_Name;

        [SerializeReference]
        private object m_Value;

        /// <summary>
        /// The name of the attribute.
        /// </summary>
        public string Name => m_Name;

        /// <summary>
        /// Gets the default value of the attribute.
        /// </summary>
        public object DefaultValue => m_Value;

        /// <summary>
        /// Gets the runtime type of the default value.
        /// </summary>
        public Type Type => DefaultValue?.GetType();

        /// <summary>
        /// Constructs an attribute with a given name and a default value
        /// </summary>
        /// <param name="name">The name of the created attribute.</param>
        /// <param name="defaultValue">The default value of the created attribute.</param>
        public Attribute(string name, object defaultValue)
        {
            m_Name = name;
            m_Value = defaultValue;
        }
    }
    /// <summary>
    /// Specifies how an attribute is going to be accessed, i.e. read, write, or both.
    /// </summary>
    [Flags]
    /*public*/ enum AttributeUsage
    {
        /// <summary>
        /// Attribute is read.
        /// </summary>
        Read = 1 << 0,
        /// <summary>
        /// Attribute is written.
        /// </summary>
        Write = 1 << 1,
        /// <summary>
        /// Attribute is both read and written.
        /// </summary>
        ReadWrite = Read | Write,
    }
}
