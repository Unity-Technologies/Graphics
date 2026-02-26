using System;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Represents a list of subordinate items with a capacity provided by an <see cref="ICountable"/> provider.
    /// </summary>
    /// <typeparam name="T">The value type of the items in the list.</typeparam>
    internal class SubordinateList<T> where T : struct
    {
        private ICountable m_CapacityProvider;
        private T[] m_Items;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubordinateList{T}"/> class with the specified capacity provider.
        /// </summary>
        /// <param name="capacityProvider">The provider that determines the capacity of the list.</param>
        public SubordinateList(ICountable capacityProvider)
        {
            m_CapacityProvider = capacityProvider;
            m_Items = new T[m_CapacityProvider.Count];
        }

        /// <summary>
        /// Gets a reference to the item at the specified <see cref="GraphDataId"/>.
        /// Resizes the internal array if the index is within the provider's capacity but exceeds the current array length.
        /// </summary>
        /// <param name="id">The <see cref="GraphDataId"/> identifying the item.</param>
        /// <returns>A reference to the item at the specified index.</returns>
        public ref T this[GraphDataId id]
        {
            get
            {
                int index = id.Index;
                if (index >= m_Items.Length)
                {
                    if (index < m_CapacityProvider.Count)
                    {
                        Resize(m_CapacityProvider.Count);
                    }
                }

                return ref m_Items[index];
            }
        }

        /// <summary>
        /// Resizes the internal array to the specified capacity.
        /// </summary>
        /// <param name="capacity">The new capacity for the internal array.</param>
        public void Resize(int capacity)
        {
            Array.Resize(ref m_Items, capacity);
        }

        /// <summary>
        /// Releases the resources used by the list and clears its contents.
        /// </summary>
        public void Release()
        {
            m_Items = Array.Empty<T>();
        }
    }
}
