using System;
using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// A class holding all transient data needed during compilation.
    /// This is designed in a generic way. Any type of compilation data can be added.
    /// Compilation passes can create and read generic compilation data (by type).
    /// </summary>
    /*public*/ class CompilationData
    {
        Dictionary<Type, object> m_Data = new();

        /// <summary>
        /// Gets the compilation data of a given type.
        /// This will throw if no data of the given type has been previously created.
        /// </summary>
        /// <typeparam name="T">The type of compilation data to get.</typeparam>
        /// <returns>the compilation data of type T.</returns>
        public T Get<T>()
        {
            return (T)m_Data[typeof(T)];
        }

        /// <summary>
        /// Gets the compilation data of a given type.
        /// Creates it if not already existing.
        /// </summary>
        /// <typeparam name="T">The type of compilation data to get or create.</typeparam>
        /// <returns>the compilation data of type T.</returns>
        public T GetOrCreate<T>() where T : new()
        {
            var key = typeof(T);
            object value;
            m_Data.TryGetValue(key, out value);
            if (value == null)
            {
                value = new T();
                m_Data.Add(key, value);
            }
            return (T)value;
        }

        /// <summary>
        /// Creates a compilation data of a given type.
        /// This throws if compilation data of this type already exists.
        /// </summary>
        /// <typeparam name="T">The type of compilation data to create.</typeparam>
        /// <param name="t">The initial value of the compilation data.</param>
        public void Create<T>(T t)
        {
            var key = typeof(T);
            if (m_Data.ContainsKey(key))
                throw new ArgumentException("CompilationData already exists");

            m_Data.Add(key, t);
        }

        /// <summary>
        /// Clears all compilation data.
        /// </summary>
        public void Clear()
        {
            m_Data.Clear();
        }
    }
}
