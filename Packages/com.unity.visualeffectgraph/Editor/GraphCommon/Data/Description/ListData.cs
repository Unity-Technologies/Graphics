using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Ordered data collection with elements of different types, referenced by index.
    /// </summary>
    /*public*/ class ListData : IDataDescription
    {
        List<IDataDescription> m_Datas = new();

        /// <summary>
        /// List of the data descriptions for each element.
        /// </summary>
        public ReadOnlyList<IDataDescription> Datas => m_Datas;

        /// <summary>
        /// Creates a new ListData, specifying the data description of all the elements.
        /// </summary>
        /// <param name="datas">The data description of every element of the collection.</param>
        public ListData(IEnumerable<IDataDescription> datas)
        {
            m_Datas = new(datas);
        }

        /// <inheritdoc cref="IDataDescription"/>
        public IDataDescription GetSubdata(IDataKey dataKey)
        {
            if (dataKey is IndexDataKey indexId && indexId.Index >= 0 && indexId.Index < m_Datas.Count)
            {
                return m_Datas[indexId.Index];
            }
            return null;
        }
    }
}
