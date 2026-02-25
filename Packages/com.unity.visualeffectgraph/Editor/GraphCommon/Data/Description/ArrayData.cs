using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Data collection with several elements of the same type.
    /// </summary>
    /*public*/ class ArrayData : IDataDescription
    {
        /// <summary>
        /// Number of elements contained by the collection.
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        /// Data description of a single element of the collection.
        /// </summary>
        public IDataDescription Data { get; }

        /// <summary>
        /// Creates a new ArrayData, specifying the capacity and the data description of the elements.
        /// </summary>
        /// <param name="capacity">The number of elements contained by the collection.</param>
        /// <param name="data">The data description of a single element of the collection.</param>
        public ArrayData(int capacity, IDataDescription data)
        {
            Debug.Assert(capacity >= 0);
            Debug.Assert(data != null);
            Capacity = capacity;
            Data = data;
        }

        /// <inheritdoc cref="IDataDescription"/>
        public IDataDescription GetSubdata(IDataKey dataKey)
        {
            if (dataKey is IndexDataKey indexId && indexId.Index >= 0 && indexId.Index < Capacity)
            {
                return Data;
            }
            return null;
        }
    }
}
