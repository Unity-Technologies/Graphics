using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Unordered data collection with elements of different types, referenced by any data identifier.
    /// </summary>
    /*public*/ class StructuredData : IDataDescription
    {
        Dictionary<IDataKey, IDataDescription> m_Datas = new();

        /// <summary>
        /// Adds a data element, providing the data identifier and the data description.
        /// </summary>
        /// <param name="dataKey">The data identifier for this element.</param>
        /// <param name="data">The data description for this element.</param>
        /// <returns>True if the data element was added, false otherwise (for instance, if it was already present).</returns>
        public bool AddSubdata(IDataKey dataKey, IDataDescription data)
        {
            return m_Datas.TryAdd(dataKey, data);
        }

        /// <inheritdoc cref="IDataDescription"/>
        public IDataDescription GetSubdata(IDataKey dataKey)
        {
            return m_Datas.GetValueOrDefault(dataKey);
        }

        /// <summary>
        /// Enumerates all the subdata descriptions included in this data description.
        /// </summary>
        public IEnumerable<IDataDescription> SubDataDescriptions => m_Datas.Values;

        /// <summary>
        /// Enumerates all the subdata descriptions included in this data description.
        /// </summary>
        public IEnumerable<KeyValuePair<IDataKey, IDataDescription>> SubDatas => m_Datas;
    }
}

