using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    class AttributeSourceManager
    {
        readonly Dictionary<DataViewId, IDataKey> m_AttributeSources = new();

        public void Add(DataViewId dataViewId, IDataKey attributeSourceKey) => m_AttributeSources.Add(dataViewId, attributeSourceKey);

        public bool TryGetAttributeSource(DataViewId dataViewId, out IDataKey attributeSourceKey) => m_AttributeSources.TryGetValue(dataViewId, out attributeSourceKey);

        public void Clear() => m_AttributeSources.Clear();
    }
}
